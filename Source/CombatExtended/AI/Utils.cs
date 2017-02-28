using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Verse;

namespace CombatExtended.AI
{
	//From Zhentar BetterPath Finding..
	public static class Utils
	{
		//From Zhentar BetterPath Finding..
		public static bool IsDiagonal(this IntVec3 a, IntVec3 b)
		{
			return a.x != b.x && a.z != b.z;
		}

		//From Zhentar BetterPath Finding..
		public static bool WalkableExtraFast(this int[] pathGridDirect, int index)
		{
			return pathGridDirect[index] < 10000;
		}

		//From Zhentar BetterPath Finding..
		public static bool IsIndexDiagonal(this int a, int b, Map map)
		{
			return ((a % map.Size.x != b % map.Size.x)) && ((a / map.Size.x != b / map.Size.x));
		}


		//From Zhentar BetterPath Finding..
		public static Func<TObject, TValue> GetFieldAccessor<TObject, TValue>(string fieldName)
		{
			ParameterExpression param = Expression.Parameter(typeof(TObject), "arg");
			MemberExpression member = Expression.Field(param, fieldName);
			LambdaExpression lambda = Expression.Lambda(typeof(Func<TObject, TValue>), member, param);
			Func<TObject, TValue> compiled = (Func<TObject, TValue>)lambda.Compile();
			return compiled;
		}

		//From Zhentar BetterPath Finding..
		public static Func<object, TValue> GetFieldAccessor<TValue>(Type objectType, string fieldName)
		{
			var param = Expression.Parameter(typeof(object), "arg");
			var cast = Expression.Convert(param, objectType);
			var member = Expression.Field(cast, fieldName);
			var lambda = Expression.Lambda(typeof(Func<object, TValue>), member, param);
			var compiled = (Func<object, TValue>)lambda.Compile();
			return compiled;
		}

		//From Zhentar BetterPath Finding..
		public static IEnumerable<int> PathableNeighborIndices(this int index, Map map)
		{
			int mapX = map.Size.x;
			var eastInBounds = (index % mapX > 0) && map.pathGrid.pathGrid.WalkableExtraFast(index - 1);
			var westInBounds = (index % mapX < (mapX - 1)) && map.pathGrid.pathGrid.WalkableExtraFast(index + 1);
			if (index > mapX && map.pathGrid.pathGrid.WalkableExtraFast(index - mapX)) //North in bounds
			{
				yield return index - mapX;
				if (eastInBounds) { yield return index - mapX - 1; }
				if (westInBounds) { yield return index - mapX + 1; }
			}
			if (eastInBounds) { yield return index - 1; }
			if (westInBounds) { yield return index + 1; }
			if ((index / mapX) < (map.Size.z - 1) && map.pathGrid.pathGrid.WalkableExtraFast(index + mapX)) //South in bounds
			{
				yield return index + mapX;
				if (eastInBounds) { yield return index + mapX - 1; }
				if (westInBounds) { yield return index + mapX + 1; }
			}
		}

		//From Zhentar BetterPath Finding...
		public static IEnumerable<int> NeighborIndices(this int index, Map map)
		{
			int mapX = map.Size.x;
			var eastInBounds = (index % mapX > 0);
			var westInBounds = (index % mapX < (mapX - 1));
			if (index > mapX) //North in bounds
			{
				yield return index - mapX;
				if (eastInBounds) { yield return index - mapX - 1; }
				if (westInBounds) { yield return index - mapX + 1; }
			}
			if (eastInBounds) { yield return index - 1; }
			if (westInBounds) { yield return index + 1; }
			if ((index / mapX) < (map.Size.z - 1)) //South in bounds
			{
				yield return index + mapX;
				if (eastInBounds) { yield return index + mapX - 1; }
				if (westInBounds) { yield return index + mapX + 1; }
			}
		}
	}

}
