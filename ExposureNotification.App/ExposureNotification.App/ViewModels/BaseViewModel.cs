using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ContactTracing.App.ViewModels
{
	public class BaseViewModel : INotifyPropertyChanged
	{
		protected void NotifyPropertyChanged(string propertyName)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
