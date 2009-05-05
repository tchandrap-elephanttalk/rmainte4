using System;
using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using System.ComponentModel;

namespace iida.rmainte4.Controls
{
	public class DragAndDropListView : ListView
	{
        public DragAndDropListView()
            : base()
        {
        }
        
		[Category("Behavior")]
		public bool AllowReorder
		{
			get { return _allowReorder; }
			set { _allowReorder = value; }
		}
        private bool _allowReorder = false;

		[Category("Appearance")]
		public Color LineColor
		{
			get { return _lineColor; }
			set { _lineColor = value; }
		}
        private Color _lineColor = Color.Gray;

        private ListViewItem _previousItem;


		protected override void OnDragDrop(DragEventArgs e)
		{
			if(!_allowReorder)
			{
				base.OnDragDrop(e);
				return;
			}

			// get the currently hovered row that the items will be dragged to
			Point clientPoint = base.PointToClient(new Point(e.X, e.Y));
			ListViewItem hoverItem = base.GetItemAt(clientPoint.X, clientPoint.Y);

            if (!e.Data.GetDataPresent(typeof (ListViewDragItemData)) || ((ListViewDragItemData)e.Data.GetData(typeof (ListViewDragItemData))).ListView == null || ((ListViewDragItemData)e.Data.GetData(typeof (ListViewDragItemData))).DragItems.Count == 0)
            {
                return;
            }

            // retrieve the drag item data
			ListViewDragItemData data = (ListViewDragItemData) e.Data.GetData(typeof(ListViewDragItemData).ToString());

			if(hoverItem == null)
			{
				// the user does not wish to re-order the items, just append to the end
				for(int i=0; i<data.DragItems.Count; i++)
				{
					ListViewItem newItem = (ListViewItem) data.DragItems[i];
					base.Items.Add(newItem);
				}
			}
			else
			{
				// the user wishes to re-order the items

				// get the index of the hover item
				int hoverIndex = hoverItem.Index;

				// determine if the items to be dropped are from
				// this list view. If they are, perform a hack
				// to increment the hover index so that the items
				// get moved properly.
				if(this == data.ListView)
				{
					if(hoverIndex > base.SelectedItems[0].Index)
						hoverIndex++;
				}

				// insert the new items into the list view
				// by inserting the items reversely from the array list
				for(int i=data.DragItems.Count - 1; i >= 0; i--)
				{
					ListViewItem newItem = (ListViewItem) data.DragItems[i];
					base.Items.Insert(hoverIndex, newItem);
				}
			}

			// remove all the selected items from the previous list view
			// if the list view was found
			if(data.ListView != null)
			{
				foreach(ListViewItem itemToRemove in data.ListView.SelectedItems)
				{
					data.ListView.Items.Remove(itemToRemove);
				}
			}

			// set the back color of the previous item, then nullify it
			if(_previousItem != null)
			{
				_previousItem = null;
			}

			this.Invalidate();

			// call the base on drag drop to raise the event
			base.OnDragDrop (e);
		}

		protected override void OnDragOver(DragEventArgs e)
		{
			if(!_allowReorder)
			{
				base.OnDragOver(e);
				return;
			}

			if(!e.Data.GetDataPresent(typeof (ListViewDragItemData)))
			{
				// the item(s) being dragged do not have any data associated
				e.Effect = DragDropEffects.None;
				return;
			}

			if(base.Items.Count > 0)
			{
				// get the currently hovered row that the items will be dragged to
				Point clientPoint = base.PointToClient(new Point(e.X, e.Y));
				ListViewItem hoverItem = base.GetItemAt(clientPoint.X, clientPoint.Y);

				Graphics g = this.CreateGraphics();

				if(hoverItem == null)
				{
					// no item was found, so no drop should take place
					e.Effect = DragDropEffects.Move;

					if(_previousItem != null)
					{
						_previousItem = null;
						Invalidate();
					}

					hoverItem = base.Items[base.Items.Count - 1];
						
					if(this.View == View.Details || this.View == View.List)
					{
						g.DrawLine(new Pen(_lineColor, 2), new Point(hoverItem.Bounds.X, hoverItem.Bounds.Y + hoverItem.Bounds.Height), new Point(hoverItem.Bounds.X + this.Bounds.Width, hoverItem.Bounds.Y + hoverItem.Bounds.Height));
						g.FillPolygon(new SolidBrush(_lineColor), new Point[] {new Point(hoverItem.Bounds.X, hoverItem.Bounds.Y + hoverItem.Bounds.Height - 5), new Point(hoverItem.Bounds.X + 5, hoverItem.Bounds.Y + hoverItem.Bounds.Height), new Point(hoverItem.Bounds.X, hoverItem.Bounds.Y + hoverItem.Bounds.Height + 5)});
						g.FillPolygon(new SolidBrush(_lineColor), new Point[] {new Point(this.Bounds.Width - 4, hoverItem.Bounds.Y + hoverItem.Bounds.Height - 5), new Point(this.Bounds.Width - 9, hoverItem.Bounds.Y + hoverItem.Bounds.Height), new Point(this.Bounds.Width - 4, hoverItem.Bounds.Y + hoverItem.Bounds.Height + 5)});
					}
					else
					{
						g.DrawLine(new Pen(_lineColor, 2), new Point(hoverItem.Bounds.X + hoverItem.Bounds.Width, hoverItem.Bounds.Y), new Point(hoverItem.Bounds.X + hoverItem.Bounds.Width, hoverItem.Bounds.Y + hoverItem.Bounds.Height));
						g.FillPolygon(new SolidBrush(_lineColor), new Point[] {new Point(hoverItem.Bounds.X + hoverItem.Bounds.Width - 5, hoverItem.Bounds.Y), new Point(hoverItem.Bounds.X + hoverItem.Bounds.Width + 5, hoverItem.Bounds.Y), new Point(hoverItem.Bounds.X + hoverItem.Bounds.Width, hoverItem.Bounds.Y + 5)});
						g.FillPolygon(new SolidBrush(_lineColor), new Point[] {new Point(hoverItem.Bounds.X + hoverItem.Bounds.Width - 5, hoverItem.Bounds.Y + hoverItem.Bounds.Height), new Point(hoverItem.Bounds.X + hoverItem.Bounds.Width + 5, hoverItem.Bounds.Y + hoverItem.Bounds.Height), new Point(hoverItem.Bounds.X + hoverItem.Bounds.Width, hoverItem.Bounds.Y + hoverItem.Bounds.Height - 5)});
					}

					// call the base OnDragOver event
					base.OnDragOver(e);

					return;
				}

				// determine if the user is currently hovering over a new
				// item. If so, set the previous item's back color back
				// to the default color.
				if((_previousItem != null && _previousItem != hoverItem) || _previousItem == null)
				{
					this.Invalidate();
				}
			
				// set the background color of the item being hovered
				// and assign the previous item to the item being hovered
				//hoverItem.BackColor = Color.Beige;
				_previousItem = hoverItem;

				if(this.View == View.Details || this.View == View.List)
				{
					g.DrawLine(new Pen(_lineColor, 2), new Point(hoverItem.Bounds.X, hoverItem.Bounds.Y), new Point(hoverItem.Bounds.X + this.Bounds.Width, hoverItem.Bounds.Y));
					g.FillPolygon(new SolidBrush(_lineColor), new Point[] {new Point(hoverItem.Bounds.X, hoverItem.Bounds.Y - 5), new Point(hoverItem.Bounds.X + 5, hoverItem.Bounds.Y), new Point(hoverItem.Bounds.X, hoverItem.Bounds.Y + 5)});
					g.FillPolygon(new SolidBrush(_lineColor), new Point[] {new Point(this.Bounds.Width - 4, hoverItem.Bounds.Y - 5), new Point(this.Bounds.Width - 9, hoverItem.Bounds.Y), new Point(this.Bounds.Width - 4, hoverItem.Bounds.Y + 5)});
				}
				else
				{
					g.DrawLine(new Pen(_lineColor, 2), new Point(hoverItem.Bounds.X, hoverItem.Bounds.Y), new Point(hoverItem.Bounds.X, hoverItem.Bounds.Y + hoverItem.Bounds.Height));
					g.FillPolygon(new SolidBrush(_lineColor), new Point[] {new Point(hoverItem.Bounds.X - 5, hoverItem.Bounds.Y), new Point(hoverItem.Bounds.X + 5, hoverItem.Bounds.Y), new Point(hoverItem.Bounds.X, hoverItem.Bounds.Y + 5)});
					g.FillPolygon(new SolidBrush(_lineColor), new Point[] {new Point(hoverItem.Bounds.X - 5, hoverItem.Bounds.Y + hoverItem.Bounds.Height), new Point(hoverItem.Bounds.X + 5, hoverItem.Bounds.Y + hoverItem.Bounds.Height), new Point(hoverItem.Bounds.X, hoverItem.Bounds.Y + hoverItem.Bounds.Height - 5)});
				}

				// go through each of the selected items, and if any of the
				// selected items have the same index as the item being
				// hovered, disable dropping.
				foreach(ListViewItem itemToMove in base.SelectedItems)
				{
					if(itemToMove.Index == hoverItem.Index)
					{
						e.Effect = DragDropEffects.None;
						hoverItem.EnsureVisible();
						return;
					}
				}

				// ensure that the hover item is visible
				hoverItem.EnsureVisible();
			}

			// everything is fine, allow the user to move the items
			e.Effect = DragDropEffects.Move;

			// call the base OnDragOver event
			base.OnDragOver(e);
		}

		protected override void OnDragEnter(DragEventArgs e)
		{
			if(!_allowReorder)
			{
				base.OnDragEnter(e);
				return;
			}

			if(!e.Data.GetDataPresent(typeof(ListViewDragItemData)))
			{
				// the item(s) being dragged do not have any data associated
				e.Effect = DragDropEffects.None;
				return;
			}

			// everything is fine, allow the user to move the items
			e.Effect = DragDropEffects.Move;

			// call the base OnDragEnter event
			base.OnDragEnter(e);
		}

		protected override void OnItemDrag(ItemDragEventArgs e)
		{
			if(!_allowReorder)
			{
				base.OnItemDrag(e);
				return;
			}

			// call the DoDragDrop method
			base.DoDragDrop(GetDataForDragDrop(), DragDropEffects.Move);

			// call the base OnItemDrag event
			base.OnItemDrag(e);
		}

		protected override void OnLostFocus(EventArgs e)
		{
			// reset the selected items background and remove the previous item
			ResetOutOfRange();

			Invalidate();

			// call the OnLostFocus event
			base.OnLostFocus(e);
		}

		protected override void OnDragLeave(EventArgs e)
		{
			// reset the selected items background and remove the previous item
			ResetOutOfRange();

			Invalidate();

			// call the base OnDragLeave event
			base.OnDragLeave(e);
		}


		private ListViewDragItemData GetDataForDragDrop()
		{
			// create a drag item data object that will be used to pass along with the drag and drop
			ListViewDragItemData data = new ListViewDragItemData(this);

			// go through each of the selected items and 
			// add them to the drag items collection
			// by creating a clone of the list item
			foreach(ListViewItem item in this.SelectedItems)
			{
				data.DragItems.Add(item.Clone());
			}

			return data;
		}

		private void ResetOutOfRange()
		{
			// determine if the previous item exists,
			// if it does, reset the background and release 
			// the previous item
			if(_previousItem != null)
			{
				_previousItem = null;
			}

		}

        private class ListViewDragItemData
        {
            public ListViewDragItemData(DragAndDropListView listView)
            {
                _listView = listView;
                _dragItems = new ArrayList();
            }

            public DragAndDropListView ListView
            {
                get { return _listView; }
            }
            private DragAndDropListView _listView;

            public ArrayList DragItems
            {
                get { return _dragItems; }
            }
            private ArrayList _dragItems;


        }





	}






}
