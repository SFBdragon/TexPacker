
/*
 	Based on the Public Domain MaxRectsBinPack.cpp source by Jukka Jylänki
 	https://github.com/juj/RectangleBinPack/
 
 	Ported to C# by Sven Magnus
 	This version is also public domain - do whatever you want with it.
	http://wiki.unity3d.com/index.php/MaxRectsBinPack

	Modified by SFBdragon/Shaun Beautement for integration purposes.
	This version as follows is thus under the liscence as specified by the project (MIT).
*/

using System;
using System.Collections.Generic;
using SDL2;

namespace TexPacker
{
	class MaxRectsBinPack
	{
		public int BinWidth = 0;
		public int BinHeight = 0;
		public bool AllowRotations;

		public List<SDL.SDL_Rect> UsedRectangles = new List<SDL.SDL_Rect>();
		public List<SDL.SDL_Rect> FreeRectangles = new List<SDL.SDL_Rect>();

		public enum FreeRectChoiceHeuristic
		{
			RectBestShortSideFit, ///< -BSSF: Positions the rectangle against the short side of a free rectangle into which it fits the best.
			RectBestLongSideFit, ///< -BLSF: Positions the rectangle against the long side of a free rectangle into which it fits the best.
			RectBestAreaFit, ///< -BAF: Positions the rectangle into the smallest free rect into which it fits.
			RectBottomLeftRule, ///< -BL: Does the Tetris placement.
			RectContactPointRule ///< -CP: Choosest the placement where the rectangle touches other rects as much as possible.
		};

		public MaxRectsBinPack(int width, int height, bool rotations = true)
		{
			Init(width, height, rotations);
		}

		public void Init(int width, int height, bool rotations = true)
		{
			BinWidth = width;
			BinHeight = height;
			AllowRotations = rotations;

			SDL.SDL_Rect n = new SDL.SDL_Rect {
				x = 0,
				y = 0,
				w = width,
				h = height
			};

			UsedRectangles.Clear();

			FreeRectangles.Clear();
			FreeRectangles.Add(n);
		}

		public SDL.SDL_Rect Insert(int width, int height, FreeRectChoiceHeuristic method)
		{
			SDL.SDL_Rect newNode = new SDL.SDL_Rect();
			int score1 = 0; // Unused in this function. We don't need to know the score after finding the position.
			int score2 = 0;
			switch (method) {
				case FreeRectChoiceHeuristic.RectBestShortSideFit:
					newNode = FindPositionForNewNodeBestShortSideFit(width, height, ref score1, ref score2);
					break;
				case FreeRectChoiceHeuristic.RectBottomLeftRule:
					newNode = FindPositionForNewNodeBottomLeft(width, height, ref score1, ref score2);
					break;
				case FreeRectChoiceHeuristic.RectContactPointRule:
					newNode = FindPositionForNewNodeContactPoint(width, height, ref score1);
					break;
				case FreeRectChoiceHeuristic.RectBestLongSideFit:
					newNode = FindPositionForNewNodeBestLongSideFit(width, height, ref score2, ref score1);
					break;
				case FreeRectChoiceHeuristic.RectBestAreaFit:
					newNode = FindPositionForNewNodeBestAreaFit(width, height, ref score1, ref score2);
					break;
			}

			if (newNode.h == 0)
				return newNode;

			int numRectanglesToProcess = FreeRectangles.Count;
			for (int i = 0; i < numRectanglesToProcess; ++i) {
				if (SplitFreeNode(FreeRectangles[i], ref newNode)) {
					FreeRectangles.RemoveAt(i);
					--i;
					--numRectanglesToProcess;
				}
			}

			PruneFreeList();

			UsedRectangles.Add(newNode);
			return newNode;
		}

		public void Insert(List<SDL.SDL_Rect> rects, FreeRectChoiceHeuristic method)
		{
			while (rects.Count > 0) {
				int bestScore1 = int.MaxValue;
				int bestScore2 = int.MaxValue;
				int bestRectIndex = -1;
				SDL.SDL_Rect bestNode = new SDL.SDL_Rect();

				for (int i = 0; i < rects.Count; ++i) {
					int score1 = 0;
					int score2 = 0;
					SDL.SDL_Rect newNode = ScoreRect(rects[i].w, rects[i].h, method, ref score1, ref score2);

					if (score1 < bestScore1 || score1 == bestScore1 && score2 < bestScore2) {
						bestScore1 = score1;
						bestScore2 = score2;
						bestNode = newNode;
						bestRectIndex = i;
					}
				}

				if (bestRectIndex == -1)
					return;

				PlaceRect(bestNode);
				rects.RemoveAt(bestRectIndex);
			}
		}

		internal void PlaceRect(SDL.SDL_Rect node)
		{
			int numRectanglesToProcess = FreeRectangles.Count;
			for (int i = 0; i < numRectanglesToProcess; ++i) {
				if (SplitFreeNode(FreeRectangles[i], ref node)) {
					FreeRectangles.RemoveAt(i);
					--i;
					--numRectanglesToProcess;
				}
			}

			PruneFreeList();

			UsedRectangles.Add(node);
		}

		internal SDL.SDL_Rect ScoreRect(int width, int height, FreeRectChoiceHeuristic method, ref int score1, ref int score2)
		{
			SDL.SDL_Rect newNode = new SDL.SDL_Rect();
			score1 = int.MaxValue;
			score2 = int.MaxValue;
			switch (method) {
				case FreeRectChoiceHeuristic.RectBestShortSideFit:
					newNode = FindPositionForNewNodeBestShortSideFit(width, height, ref score1, ref score2);
					break;
				case FreeRectChoiceHeuristic.RectBottomLeftRule:
					newNode = FindPositionForNewNodeBottomLeft(width, height, ref score1, ref score2);
					break;
				case FreeRectChoiceHeuristic.RectContactPointRule:
					newNode = FindPositionForNewNodeContactPoint(width, height, ref score1);
					score1 = -score1; // Reverse since we are minimizing, but for contact point score bigger is better.
					break;
				case FreeRectChoiceHeuristic.RectBestLongSideFit:
					newNode = FindPositionForNewNodeBestLongSideFit(width, height, ref score2, ref score1);
					break;
				case FreeRectChoiceHeuristic.RectBestAreaFit:
					newNode = FindPositionForNewNodeBestAreaFit(width, height, ref score1, ref score2);
					break;
			}

			// Cannot fit the current rectangle.
			if (newNode.h == 0) {
				score1 = int.MaxValue;
				score2 = int.MaxValue;
			}

			return newNode;
		}

		/// Computes the ratio of used surface area.
		public float Occupancy()
		{
			ulong usedSurfaceArea = 0;
			for (int i = 0; i < UsedRectangles.Count; ++i)
				usedSurfaceArea += (uint)UsedRectangles[i].w * (uint)UsedRectangles[i].h;

			return (float)usedSurfaceArea / (BinWidth * BinHeight);
		}

		SDL.SDL_Rect FindPositionForNewNodeBottomLeft(int width, int height, ref int bestY, ref int bestX)
		{
			SDL.SDL_Rect bestNode = new SDL.SDL_Rect();
			//memset(bestNode, 0, sizeof(SDL.SDL_Rect));

			bestY = int.MaxValue;

			for (int i = 0; i < FreeRectangles.Count; ++i) {
				// Try to place the rectangle in upright (non-flipped) orientation.
				if (FreeRectangles[i].w >= width && FreeRectangles[i].h >= height) {
					int topSideY = FreeRectangles[i].y + height;
					if (topSideY < bestY || topSideY == bestY && FreeRectangles[i].x < bestX) {
						bestNode.x = FreeRectangles[i].x;
						bestNode.y = FreeRectangles[i].y;
						bestNode.w = width;
						bestNode.h = height;
						bestY = topSideY;
						bestX = FreeRectangles[i].x;
					}
				}
				if (AllowRotations && FreeRectangles[i].w >= height && FreeRectangles[i].h >= width) {
					int topSideY = FreeRectangles[i].y + width;
					if (topSideY < bestY || topSideY == bestY && FreeRectangles[i].x < bestX) {
						bestNode.x = FreeRectangles[i].x;
						bestNode.y = FreeRectangles[i].y;
						bestNode.w = height;
						bestNode.h = width;
						bestY = topSideY;
						bestX = FreeRectangles[i].x;
					}
				}
			}
			return bestNode;
		}

		SDL.SDL_Rect FindPositionForNewNodeBestShortSideFit(int width, int height, ref int bestShortSideFit, ref int bestLongSideFit)
		{
			SDL.SDL_Rect bestNode = new SDL.SDL_Rect();
			//memset(&bestNode, 0, sizeof(SDL.SDL_Rect));

			bestShortSideFit = int.MaxValue;

			for (int i = 0; i < FreeRectangles.Count; ++i) {
				// Try to place the rectangle in upright (non-flipped) orientation.
				if (FreeRectangles[i].w >= width && FreeRectangles[i].h >= height) {
					int leftoverHoriz = Math.Abs(FreeRectangles[i].w - width);
					int leftoverVert = Math.Abs(FreeRectangles[i].h - height);
					int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
					int longSideFit = Math.Max(leftoverHoriz, leftoverVert);

					if (shortSideFit < bestShortSideFit || shortSideFit == bestShortSideFit && longSideFit < bestLongSideFit) {
						bestNode.x = FreeRectangles[i].x;
						bestNode.y = FreeRectangles[i].y;
						bestNode.w = width;
						bestNode.h = height;
						bestShortSideFit = shortSideFit;
						bestLongSideFit = longSideFit;
					}
				}

				if (AllowRotations && FreeRectangles[i].w >= height && FreeRectangles[i].h >= width) {
					int flippedLeftoverHoriz = Math.Abs(FreeRectangles[i].w - height);
					int flippedLeftoverVert = Math.Abs(FreeRectangles[i].h - width);
					int flippedShortSideFit = Math.Min(flippedLeftoverHoriz, flippedLeftoverVert);
					int flippedLongSideFit = Math.Max(flippedLeftoverHoriz, flippedLeftoverVert);

					if (flippedShortSideFit < bestShortSideFit || flippedShortSideFit == bestShortSideFit && flippedLongSideFit < bestLongSideFit) {
						bestNode.x = FreeRectangles[i].x;
						bestNode.y = FreeRectangles[i].y;
						bestNode.w = height;
						bestNode.h = width;
						bestShortSideFit = flippedShortSideFit;
						bestLongSideFit = flippedLongSideFit;
					}
				}
			}
			return bestNode;
		}

		SDL.SDL_Rect FindPositionForNewNodeBestLongSideFit(int width, int height, ref int bestShortSideFit, ref int bestLongSideFit)
		{
			SDL.SDL_Rect bestNode = new SDL.SDL_Rect();
			//memset(&bestNode, 0, sizeof(SDL.SDL_Rect));

			bestLongSideFit = int.MaxValue;

			for (int i = 0; i < FreeRectangles.Count; ++i) {
				// Try to place the rectangle in upright (non-flipped) orientation.
				if (FreeRectangles[i].w >= width && FreeRectangles[i].h >= height) {
					int leftoverHoriz = Math.Abs(FreeRectangles[i].w - width);
					int leftoverVert = Math.Abs(FreeRectangles[i].h - height);
					int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
					int longSideFit = Math.Max(leftoverHoriz, leftoverVert);

					if (longSideFit < bestLongSideFit || longSideFit == bestLongSideFit && shortSideFit < bestShortSideFit) {
						bestNode.x = FreeRectangles[i].x;
						bestNode.y = FreeRectangles[i].y;
						bestNode.w = width;
						bestNode.h = height;
						bestShortSideFit = shortSideFit;
						bestLongSideFit = longSideFit;
					}
				}

				if (AllowRotations && FreeRectangles[i].w >= height && FreeRectangles[i].h >= width) {
					int leftoverHoriz = Math.Abs(FreeRectangles[i].w - height);
					int leftoverVert = Math.Abs(FreeRectangles[i].h - width);
					int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
					int longSideFit = Math.Max(leftoverHoriz, leftoverVert);

					if (longSideFit < bestLongSideFit || longSideFit == bestLongSideFit && shortSideFit < bestShortSideFit) {
						bestNode.x = FreeRectangles[i].x;
						bestNode.y = FreeRectangles[i].y;
						bestNode.w = height;
						bestNode.h = width;
						bestShortSideFit = shortSideFit;
						bestLongSideFit = longSideFit;
					}
				}
			}
			return bestNode;
		}

		SDL.SDL_Rect FindPositionForNewNodeBestAreaFit(int width, int height, ref int bestAreaFit, ref int bestShortSideFit)
		{
			SDL.SDL_Rect bestNode = new SDL.SDL_Rect();
			//memset(&bestNode, 0, sizeof(SDL.SDL_Rect));

			bestAreaFit = int.MaxValue;

			for (int i = 0; i < FreeRectangles.Count; ++i) {
				int areaFit = FreeRectangles[i].w * FreeRectangles[i].h - width * height;

				// Try to place the rectangle in upright (non-flipped) orientation.
				if (FreeRectangles[i].w >= width && FreeRectangles[i].h >= height) {
					int leftoverHoriz = Math.Abs(FreeRectangles[i].w - width);
					int leftoverVert = Math.Abs(FreeRectangles[i].h - height);
					int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);

					if (areaFit < bestAreaFit || areaFit == bestAreaFit && shortSideFit < bestShortSideFit) {
						bestNode.x = FreeRectangles[i].x;
						bestNode.y = FreeRectangles[i].y;
						bestNode.w = width;
						bestNode.h = height;
						bestShortSideFit = shortSideFit;
						bestAreaFit = areaFit;
					}
				}

				if (AllowRotations && FreeRectangles[i].w >= height && FreeRectangles[i].h >= width) {
					int leftoverHoriz = Math.Abs(FreeRectangles[i].w - height);
					int leftoverVert = Math.Abs(FreeRectangles[i].h - width);
					int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);

					if (areaFit < bestAreaFit || areaFit == bestAreaFit && shortSideFit < bestShortSideFit) {
						bestNode.x = FreeRectangles[i].x;
						bestNode.y = FreeRectangles[i].y;
						bestNode.w = height;
						bestNode.h = width;
						bestShortSideFit = shortSideFit;
						bestAreaFit = areaFit;
					}
				}
			}
			return bestNode;
		}

		/// Returns 0 if the two intervals i1 and i2 are disjoint, or the length of their overlap otherwise.
		static int CommonIntervalLength(int i1start, int i1end, int i2start, int i2end)
			=> i1end < i2start || i2end < i1start ? 0 : Math.Min(i1end, i2end) - Math.Max(i1start, i2start);

		int ContactPointScoreNode(int x, int y, int width, int height)
		{
			int score = 0;

			if (x == 0 || x + width == BinWidth)
				score += height;
			if (y == 0 || y + height == BinHeight)
				score += width;

			for (int i = 0; i < UsedRectangles.Count; ++i) {
				if (UsedRectangles[i].x == x + width || UsedRectangles[i].x + UsedRectangles[i].w == x)
					score += CommonIntervalLength((int)UsedRectangles[i].y, (int)UsedRectangles[i].y + (int)UsedRectangles[i].h, y, y + height);
				if (UsedRectangles[i].y == y + height || UsedRectangles[i].y + UsedRectangles[i].h == y)
					score += CommonIntervalLength((int)UsedRectangles[i].x, (int)UsedRectangles[i].x + (int)UsedRectangles[i].w, x, x + width);
			}
			return score;
		}

		SDL.SDL_Rect FindPositionForNewNodeContactPoint(int width, int height, ref int bestContactScore)
		{
			SDL.SDL_Rect bestNode = new SDL.SDL_Rect();
			//memset(&bestNode, 0, sizeof(SDL.SDL_Rect));

			bestContactScore = -1;

			for (int i = 0; i < FreeRectangles.Count; ++i) {
				// Try to place the rectangle in upright (non-flipped) orientation.
				if (FreeRectangles[i].w >= width && FreeRectangles[i].h >= height) {
					int score = ContactPointScoreNode((int)FreeRectangles[i].x, (int)FreeRectangles[i].y, width, height);
					if (score > bestContactScore) {
						bestNode.x = (int)FreeRectangles[i].x;
						bestNode.y = (int)FreeRectangles[i].y;
						bestNode.w = width;
						bestNode.h = height;
						bestContactScore = score;
					}
				}
				if (AllowRotations && FreeRectangles[i].w >= height && FreeRectangles[i].h >= width) {
					int score = ContactPointScoreNode((int)FreeRectangles[i].x, (int)FreeRectangles[i].y, height, width);
					if (score > bestContactScore) {
						bestNode.x = (int)FreeRectangles[i].x;
						bestNode.y = (int)FreeRectangles[i].y;
						bestNode.w = height;
						bestNode.h = width;
						bestContactScore = score;
					}
				}
			}
			return bestNode;
		}

		bool SplitFreeNode(SDL.SDL_Rect freeNode, ref SDL.SDL_Rect usedNode)
		{
			// Test with SAT if the rectangles even intersect.
			if (usedNode.x >= freeNode.x + freeNode.w || usedNode.x + usedNode.w <= freeNode.x ||
				usedNode.y >= freeNode.y + freeNode.h || usedNode.y + usedNode.h <= freeNode.y) {
				return false;
			}

			if (usedNode.x < freeNode.x + freeNode.w && usedNode.x + usedNode.w > freeNode.x) {
				// New node at the top side of the used node.
				if (usedNode.y > freeNode.y && usedNode.y < freeNode.y + freeNode.h) {
					SDL.SDL_Rect newNode = freeNode;
					newNode.h = usedNode.y - newNode.y;
					FreeRectangles.Add(newNode);
				}

				// New node at the bottom side of the used node.
				if (usedNode.y + usedNode.h < freeNode.y + freeNode.h) {
					SDL.SDL_Rect newNode = freeNode;
					newNode.y = usedNode.y + usedNode.h;
					newNode.h = freeNode.y + freeNode.h - (usedNode.y + usedNode.h);
					FreeRectangles.Add(newNode);
				}
			}

			if (usedNode.y < freeNode.y + freeNode.h && usedNode.y + usedNode.h > freeNode.y) {
				// New node at the left side of the used node.
				if (usedNode.x > freeNode.x && usedNode.x < freeNode.x + freeNode.w) {
					SDL.SDL_Rect newNode = freeNode;
					newNode.w = usedNode.x - newNode.x;
					FreeRectangles.Add(newNode);
				}

				// New node at the right side of the used node.
				if (usedNode.x + usedNode.w < freeNode.x + freeNode.w) {
					SDL.SDL_Rect newNode = freeNode;
					newNode.x = usedNode.x + usedNode.w;
					newNode.w = freeNode.x + freeNode.w - (usedNode.x + usedNode.w);
					FreeRectangles.Add(newNode);
				}
			}

			return true;
		}

		void PruneFreeList()
		{
			for (int i = 0; i < FreeRectangles.Count; ++i) {
				for (int j = i + 1; j < FreeRectangles.Count; ++j) {
					if (IsContainedIn(FreeRectangles[i], FreeRectangles[j])) {
						FreeRectangles.RemoveAt(i);
						--i;
						break;
					}
					if (IsContainedIn(FreeRectangles[j], FreeRectangles[i])) {
						FreeRectangles.RemoveAt(j);
						--j;
					}
				}
			}
		}

		static bool IsContainedIn(SDL.SDL_Rect a, SDL.SDL_Rect b)
		{
			return a.x >= b.x && a.y >= b.y
				&& a.x + a.w <= b.x + b.w
				&& a.y + a.h <= b.y + b.h;
		}
	}
}
