using System;
using System.Collections.Generic;
using System.Linq;

namespace SEE.Layout.NodeLayouts.EmptySpace
{
    /// <summary>
    /// Finds empty space in an outer object with nested other objects (obstacles)
    /// using an algorithm described in the paper "Efficient Algorithms for Identifying
    /// all Maximal Isothetic Empty Rectangles in VLSI Layout Design"
    /// by Subhas C Nandy, Bhargab B Bhattacharya and Sibabrata Ray.
    /// </summary>
    internal static class NandyEmptySpaceFinder
    {
        // CONCEPTS.
        //
        // Consider a rectangular floor on which n isothetic non-overlapping solid
        // rectangles (blocks) are placed. We will represent an isothetic rectangle
        // by an ordered tuple; The upper left corner of the rectangular floorplan
        // is assumed to be the origin of the origin of our co-ordinate system with
        // X and Y axes running towards the right and the bottom, respectively.
        //
        // An isothetic rectangle is a rectangle which is formed by isothetic
        // (straight) lines parallel to X or Y axis.
        //
        // A blank isothetic rectangle R in a given layout is called a maximal empty
        // rectangle (MER) if
        // (a) R does not intersect any other solid block in the given layout and
        // (b) any other empty rectangle with property(a) does not contain R.
        //
        // Two solid blocks P and Q are said to be rectilinearly visible if there
        // exists an isothetic line which intersects P and Q and does not intersect
        // any other solid block in between.
        // Now we classify MERs in two groups: namely type-A and type-B as follows.
        // An MER R is said to be of type-A if
        // (a) R does not touch any boundary of the chip floor and
        // (b) no two solid blocks, one touching the north (east) side and other
        //     touching the south (west) boundary of R, have rectilinear-visibility.
        //
        // An MER is of type-B if it is not of type-A.
        //
        // To enumerate type-B MER's, we use the concept of maximal horizontal strips
        // for describing a floorplan layout using comer stitching data structure.
        // In this data structure the entire blank area in a floorplan is represented
        // by a set of disjoint isothetic rectangular space blocks or tiles called maximal
        // horizontal strips. One can easily find such a partition by drawing lines parallel
        // to the X-axis that touch the top and bottom of every solid rectangle. Similarly,
        // one can think of maximal vertical strip representation of the blank area.
        //
        // Lemma I: An MER is of type-B iff it completely contains at least one maximal
        // horizontal or vertical strip.
        //
        // Let R denote the number of all MER's in a given layout.
        // Now, we introduce the concept of a window and formulate a new algorithm for
        // identifying all MER's with worst case time complexity O(n*log(n) + R ) and space
        // complexity O(n). The average case time complexity turns out to be O(n*log(n)).
        //
        // To identify all MER's, we introduce the concept of a window, which is somewhat
        // analogous to a horizontal peeping slot. Windows are generated and killed
        // dynamically when the solid blocks in the floorplan are processed and their
        // data base is managed by the interval tree.
        // Two types of windows, primary and secondary, are generated while processing
        // the bottom and top of solid blocks respectively.
        //
        // Processing the bottom of a solid block:
        //
        // Consider a solid block whose south side (bottom) s lies at height (Y-co-ordinate) h.
        // Consider a point p on s and draw a line from p to the left, parallel to the X-axis,
        // till it hits the east side of a solid block or the west boundary of the floor.
        // Similarly, extend the line from p to the right. Let l and r denote the X-co-ordinates
        // of the above hit-points.
        // A primary window is said to be generated in this event and is represented by an
        // ordered tuple ([1, r], h), where[1, r] is a horizontal interval and h denotes
        // the height where it was originated. The roof (north boundary) of the floorplan
        // is treated as the south side of a dummy solid block.
        //
        // Insertion of a window S([1,r],h) in the interval tree is solely dictated by the
        // interval [1,r]: When an interval [1, r] is to be inserted in T, a top-down scan
        // is made starting from the root of T till it finds a node w when the condition
        // l <= d(w) <= r is satisfied for the first time. Window S is then attached to
        // node w, i.e., 1, r and h are inserted into the appropriate node of the linked
        // list w.L, if it is not already there. Similarly, the corresponding node in the
        // list w.L is removed while deleting a window. Notice that the number of nodes
        // (leaves plus internal) in T remains invariant when windows are inserted or
        // deleted from T.
        //
        // Life-span of windows:
        //
        // Assume that a window([1, r], h) has been generated by the above process.
        // Consider a curtain coinciding with the interval [l, r] at height h and
        // let it "fall down". The window remains active until the falling curtain
        // first hits the top of some solid block or the bottom of the floor and then
        // becomes inactive. All active windows are kept in the interval tree and the
        // inactive ones are deleted.
        //
        // Processing the top of a solid block:
        //
        // Consider a window S([1, r], h) which remains active till it strikes the top
        // of a solid block C[(a1, b1), (a2, b2)]. Before it dies down, it returns
        // an MER [(1, h), (r, b1)] and splits itself to give birth to either zero
        // or one or two new windows whose intervals are subsumed by the interval [1, r]
        // of the parent window S and whose heights are inherited. These windows are
        // called secondary windows, which in turn, may give birth to other secondary
        // windows. The following cases may now arise:
        //
        // (1) If l >= a1 and r <= a2, then S dies leaving no offspring.
        // (2) If 1 >= a1 and r > a2 , then S dies and a secondary window ([a2, r], h)
        //     is generated.
        // (3) If 1 < a1 and r <= a2, then S dies and window ([1, a1], h) is generated.
        // (3) If 1 < a1 and r > a2, then two windows ([l, a1], h) and ([a2, r], h) are
        //     generated while S disappears
        //
        // If an active window strikes more than one solid block whose tops lie at the
        // same height, a slight modification is required. For the first solid block
        // from the left, the MER is generated and splitting of parent window takes place
        // as usual. For the remaining solid blocks which are hit by the window, only
        // splitting of the current window takes place. Similarly, if the bottom boundaries
        // of several solid blocks are aligned and they qualify for the same primary window,
        // only one window is generated instead of many.
        //
        // Processing of Window and Related Properties:
        //
        // Since windows are generated and killed dynamically, the set of active windows
        // present in the interval tree at any instant of time and their interrelationship
        // heavily depend on the sequence how the top and bottom boundaries of different
        // solid blocks are processed.Given a floorplan with solid blocks, we first create
        // an empty interval tree T. Then we process the roof of the floorplan. The top
        // and bottom boundaries of all solid blocks are then processed one by one such
        // that their Y-co-ordinates are sorted in non-decreasing order.
        // Such a sequence of processing of solid blocks is called a proper sequence.
        // Furthermore, when the top of a solid block P is processed, several active
        // windows may be found to be falling on P. The top of P is said to be comptetely
        // processed when all these windows are processed with respect to P, i.e., the
        // corresponding MERss and secondary windows are generated. The process terminates
        // when every solid block is processed and all active windows hit the south boundary
        // of the floorplan. Henceforth we will assume that solid blocks are always properly
        // sequenced and the tops so far considered are completely processed. We now observe
        // the following properties.
        //
        // Processing of solid blocks in proper sequence help identify the primary windows
        // conveniently and nicely simulates the curtain-fall mechanism, as captured in the
        // following observations.
        //
        // Observation 1:
        // Let C[(a1,b1),(a2,b2)] denote a solid block whose bottom is to be
        // currently processed per proper sequencing on a rectangular floor [(0, 0), (a, b)].
        // Assume C generates a primary window S ([l, r], b2].
        // Let HH denote the set of all blocks excluding C whose tops are already processed
        // but bottoms are yet to be processed. Denote by h_il (h_ir) the X co-ordinate
        // of the left (right) boundary of a block H in HH. Then,
        //
        // l = max_i(h_ir | h_ir < a1), if {h_ir |h_ir < a2} is not the empty set;
        // otherwise l = 0.
        //
        // r = min_i(h_il |h_il > a2) if {h_il |h_il > a2} is not the empty set;
        // otherwise r = a.
        //
        // Observation 2:
        // Let C[(a1, b1),(a2, b2)] denote a solid block whose top is to be processed now,
        // per proper sequencing. Then the curtain falling from a window S([1, r], h) will
        // strike C first iff intersect([l, r], [a1, a2]) is not the empty set.
        //
        // Definition:
        // A window A([l1, r2], h2) is said to properly subsume another window
        // B([12, r2], h2) denoted by A>B if 11 <= 12 and r1 > r2, or 11 < 12 and r1 >= r2.
        // A and B are said to be disjoint if intersect([l1, r1], [12, r2]) is the empty set.
        //
        // Lemma 2: If two windows A([l1, r1], h1) and B([12, r2], h2) are simultaneously
        // active (i.e., present in the interval tree) at any instant of time and if
        // A properly subsumes B, then h1 > h2.
        //
        // Definition: A window A ([l1, r1], h1) is said to dominate another window
        // B ([12, r2], h2) if 11 = 12, r1 = r2 and h1 < h2.
        //
        // Remark 1: Let W_p and W_s denote the set of all primary and secondary windows,
        // respectively, which are present in the interval tree at any instant of time.
        // Then, no member of W_p can either be properly subsumed or dominated by any
        // member of W_s and by any other member of W_p.
        //
        // Remark 2: If a window B is dominated by another window, then B can never
        // return an MER.
        //
        // The concept of dominance thus suggests the following strategy: the secondary
        // windows which are dominated need not be inserted in the interval tree and can
        // be completely ignored.
        //
        // Lemma 3: If two windows are simultaneously active at any instant of time,
        // either one subsumes the other or they are disjoint.
        //
        // Observation 3: Let window A ([11,r1],h1) subsume window B ([12, r2],h2).
        // Now if B is attached to node p in the interval tree, then A will be
        // attached to a node q where either p and q are identical or q is an ancestor
        // of p.
        //
        // Lemma 4: If two windows A ([l1, r1], h) and B ([l2, r2], h2) are attached
        // to a node w in the interval tree T at any instant of time, either A>B or B>A
        // (A subsumes B or vice versa).
        //
        // Let A1 ([11, r1],h1), A2 ([12, r2],h2) ... Ak([lk, rk], hk) denote all the
        // windows attached to any node w in T. Clearly, h1, h2, ..., hk are all distinct.
        //
        // Theorem 3: Let h1 < h2 < ... < hk. Then, 11 >= 12 >= ... >= lk and
        // r1 <= r2 <= .... <= rk, i.e.,there exists a linear ordering: Ak > ... > A2 > A1
        // (Ak subsumes A_k-1 subsumes .. A2 subsumes A1).
        //
        // The following theorem reflects an important correlation among the set of secondary
        // windows generated by the top of a solid block.
        //
        // Consider a collection of windows which are hitting the top t of a solid block
        // P [(al, b1), (a2, b2)]. When t is completely processed, a set of secondary windows
        // is generated and let this set be C. Clearly, C contains two disjoint sets C1 and C2
        // such that all windows in C1 (C2) have the same right (left) boundary a1 (a2).
        //
        // Theorem 4: The windows in C1 (C2) are linearly ordered with respect to the
        // subsumption relation.
        //
        // Theorem 5: Every window generated during the process returns exactly one MER and
        // every existing MER is generated by at least one window.
        //
        // Theorem 6: The number of active windows that can be present in the interval tree
        // at any instant of time is at most O(n).

        // ADDITIONAL DATA STRUCTURES
        //
        // (a) A list Y, whose elements have three fields as follows :
        //   (1) Y(i).val : containing the Y co-ordinate of the top or bottom of a solid block;
        //   (2) Y(i).solid: containing the dimension of the corresponding solid block;
        //   (3) Y(i).ind: having an indicator 't' or 'b' to indicate whether it is top
        //       or bottom boundary of the associated solid block.
        //
        // List Y is sorted in non-decreasing order with respect to Y(i).val. If two or
        // more blocks have same value of Y(i).val, then they are kept in Y in
        // increasing order with respect to the X co-ordinates of their left
        // boundaries.
        //
        // (b) Two height-balanced binary search trees: TEMP_TREE_L and TEMP TREE_R.
        //     We will use AVL trees.
        //
        // When the top of a solid block is processed, the X-co-ordinates of its left
        // and right boundaries are kept in TEMP_TREE L and TEMP_TREE R respectively
        // until its bottom boundary is processed. During the generation of a primary
        // window, the left and right extemities of its interval can easily be obtained by
        // searching these two trees as indicated in Observation 1.
        //
        // Scheme of the Algorithm
        //
        // Initially an empty interval tree T is prepared with the left and right boundaries
        // of solid blocks in sorted order. A window ([0, a], 0), corresponding to the roof
        // of the floorplan [(0, 0), (a, b)], is inserted in the interval tree.
        // We then start processing solid blocks in proper sequence.
        // Let P [(a1, b1), (a2, b2)] be the current solid block whose top is to be processed.
        //
        // Search the interval tree from the root to get a node w* such that al <= d(w*) <= a2.
        // Let P_IN be the set of nodes traversed from the root to w* (excluding w*). From w*
        // scan up to leaf level to get a1 and a2.
        // Let P_L and P_R be the set of nodes along these two paths.
        // It is obvious that all active windows in T which intersect the interval [al, a2],
        // will appear with the nodes in P_IN, P_L, and P_R. As a matter of fact, all windows
        // associated with w* will intersect [a1, a2]. The windows associated with nodes in
        // P_IN, P_L and P_R other than w* may or may not intersect [a1, a2]. The search for
        // such nodes can be efficiently accomplished using the pointers w.LPTR and w.RPTR.
        // All the windows intersecting the interval [a1, a2] will generate corresponding
        // MERs and can be found in time proportional to O(n*log(n)) plus the number of
        // reported intersections with no additional search overhead.
        //
        // While processing the top of a solid block P [(al,b1), (a2,b2)] two pointers,
        // POINTER L and POINTER_R, are maintained. Initially, these pointers point to the
        // root of T. Later, POINTER_L points to the current node of the interval tree which
        // contains the last inserted secondary window with right extremity at a1 and
        // POINTER_R points to the current node containing the last inserted secondary window
        // with left extremity at a2.
        //
        // Let w be a node whose associated windows may return MER. The list of windows in w.L
        // are processed from the end of the list (i.e., the window with the largest interval
        // first and then in decreasing order) until a window non-intersecting with the
        // interval [a1, a2] is found. By Theorem 4, the secondary windows generated at this
        // moment consist of two disjoint linearly ordered sets. From Observation 3, it follows
        // that each time a new secondary window needs to be inserted in T, the search for its
        // position is to be initiated from the node currently pointed by either POINTER_L or
        // POINTER_R, down the tree. Thus all windows generated by the top of a solid block
        // can be inserted in T including the check for dominance by traversing
        // exactly two paths in T and can be effected in time O(log(n)) plus the number of
        // such windows. These two pointers are initialized to the root of T when the
        // top of the current solid block is completely processed.
        //
        // ALGORITHM
        //
        // Given a rectangular floor [(0,0),(a,b)] with n solid blocks.
        //
        // 1. create an interval tree T with the left and right boundary co-ordinates of
        //    all solid blocks;
        //
        // 2. insert a window ([0, a],0) corresponding to the roof of the floorplan.
        //
        // 3. create the sorted list Y, i.e., arrange the top and bottom boundaries of
        //    solid blocks in proper sequence. This gives the sequence of processing.
        //
        // 4. insert a in TEMP_TREE_L and 0 in TEMP_TREE R (which were initially empty)
        //
        // 5. repeat
        //
        //   let P[(a1, b1), (a1, b2)] be the current solid block
        //
        // if the top boundary of P is to be processed, perform steps 5.1 through 5.4
        // else perform step 5.5
        //
        // 5.1. scan T from the root to find the first node w* in T such that a1 <= d(w*) <= a2;
        //      form three sets of nodes P_IN, P_L, P_R of the interval tree.
        //
        // 5.2. find all windows attached to w* and to nodes in P_IN, P_L, P_R whose intervals
        //      intersect [a1, a2]. Let this set of windows be AA.
        //
        // 5.3. for each member A in AA,
        //      (a) return the corresponding MER and generate the secondary windows;
        //      (b) delete A from T and insert the secondary windows found in 5.3(a) using
        //          POINTER_L and POINTER_R as described earlier after checking dominance.
        // 5.4. insert al (a2) in TEMP TREE_R (TEMP_TREE_L);
        //
        // 5.5.
        //   (a) find the left (right) extremity of the primary window associated to the
        //       bottom of the current solid block from TEMP_TREE_R (TEMP_TREE_L)
        //       and generate the corresponding window.
        //   (b) insert the window found in 5.5(a) in T and delete al (a2) from
        //       TEMP_TREE_R (TEMP_TREE_L).
        //
        // until (top and bottom boundaries of all solid blocks are processed).
        //
        // 6. process the bottom of the floorplan as the top boundary of a dummy solid block.

        /// <summary>
        /// Returns the set of maximally large empty rectangles for given <paramref name="outerRectangle"/>
        /// containing the <paramref name="innerRectangles"/>. None of the results will overlap with any
        /// <paramref name="innerRectangles"/>. All of them will be completely contained in <paramref name="outerRectangle"/>.
        /// Together they cover the complete empty space in <paramref name="outerRectangle"/>.
        /// The elements of the results may overlap each other.
        /// </summary>
        /// <param name="outerRectangle">the outer rectangle (must not be null)</param>
        /// <param name="innerRectangles">the nested inner rectangles (must not be null)</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">thrown if one of the arguments is null</exception>
        /// <exception cref="ArgumentException">thrown if any of the <paramref name="innerRectangles"/>
        /// is not fully contained in <paramref name="outerRectangle"/></exception>
        public static IList<Rectangle> Find(Rectangle outerRectangle, IEnumerable<Rectangle> innerRectangles)
        {
            if (outerRectangle == null)
            {
                throw new ArgumentNullException(nameof(outerRectangle));
            }
            if (innerRectangles == null)
            {
                throw new ArgumentNullException(nameof(innerRectangles));
            }

            List<Rectangle> innerList = innerRectangles.ToList();

            if (!Rectangle.AreAllNested(outerRectangle, innerList))
            {
                throw new ArgumentException($"All {nameof(innerRectangles)} must be fully contained within {nameof(outerRectangle)}");
            }

            // List to hold the maximal empty rectangles found.
            List<Rectangle> maximalRects = new();

            return maximalRects;
        }
    }

    /// <summary>
    /// An interval tree (T) is a leaf-oriented balanced binary search tree where the leaf
    /// nodes from left to right hold distinct X-co-ordinates of the left and the right side
    /// of solid blocks (whose extremes are in the set {x1, x2, ...x2n}) sorted in ascending
    /// order. Each internal node w will have the following information:
    ///
    /// (i) a discriminant value d(w)=(d(w1)+d(w2))/2 , where w1 and w2 are the left and the
    /// right child of w, respectively. The discriminant value of a leaf node is the
    /// X-co-ordinate attached to it.
    ///
    /// (ii) a secondary list (w.L) of nodes with three fields L.l, L.r and L.h, sorted in
    /// increasing order with respect to L.h, is attached to each node w of T in the form
    /// of a doublylinked list with an additional direct forward link from w to the last
    /// node in the list.
    ///
    /// W.L should likely be a sorted set instead of a simple list because the same
    /// entry must not be contained more than once.
    ///
    /// A node in T is active if it contains non-empty secondary lists w.L or it has active
    /// nodes in both of its subtrees. The active nodes in the interval tree are also
    /// linked using two different pointers, w.LPTR and w.RPTR, in the form of a binary tree.
    ///
    /// </summary>
    /// <remarks>We need to be able to delete nodes in the interval tree. For this
    /// reason, we cannot use Supercluster.KDTree, which does not support deletion.</remarks>
    internal class IntervalTree
    {
        // Possible candidate: https://github.com/jamarino/IntervalTree

    }
}
