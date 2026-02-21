using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Represents the syntax tree for a WQL query and provides access to the root nodes.
    /// </summary>
    /// <typeparam name="TIndexItem">The index item type for the syntax tree nodes.</typeparam>
    public class WqlSyntaxTree<TIndexItem> : IWqlSyntaxTree<TIndexItem>
        where TIndexItem : IIndexItem
    {
        private readonly IEnumerable<WqlToken> _tokens = [];

        /// <summary>
        /// Returns the tokens associated with this syntax tree node.
        /// </summary>
        public IEnumerable<WqlToken> Tokens => _tokens;

        /// <summary>
        /// Returns the filter node of the syntax tree.
        /// </summary>
        public IWqlExpressionNode<TIndexItem> Filter { get; }

        /// <summary>
        /// Returns the order node of the syntax tree.
        /// </summary>
        public IWqlExpressionNode<TIndexItem> Order { get; }

        /// <summary>
        /// Returns the partitioning node of the syntax tree.
        /// </summary>
        public IWqlExpressionNode<TIndexItem> Partitioning { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WqlSyntaxTree{TIndexItem}"/> class.
        /// </summary>
        /// <param name="filter">The filter node (may be null).</param>
        /// <param name="order">The order node (may be null).</param>
        /// <param name="partitioning">The partitioning node (may be null).</param>
        internal WqlSyntaxTree
        (
            IWqlExpressionNode<TIndexItem> filter,
            IWqlExpressionNode<TIndexItem> order,
            IWqlExpressionNode<TIndexItem> partitioning)
        {
            Filter = filter;
            Order = order;
            Partitioning = partitioning;
        }

        /// <summary>
        /// Returns the syntax tree of the WQL query.
        /// </summary>
        public IEnumerable<IWqlExpressionNode<TIndexItem>> AbstractSyntaxTree
        {
            get
            {
                var nodes = new List<IWqlExpressionNode<TIndexItem>>();

                if (Filter is not null)
                {
                    nodes.Add(Filter);
                }

                if (Order is not null)
                {
                    nodes.Add(Order);
                }

                if (Partitioning is not null)
                {
                    nodes.Add(Partitioning);
                }

                return nodes;
            }
        }
    }
}