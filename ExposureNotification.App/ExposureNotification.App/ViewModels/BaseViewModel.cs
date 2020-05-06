using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Xamarin.Forms;

namespace ExposureNotification.App.ViewModels
{
	public class BaseViewModel : INotifyPropertyChanged
	{
		protected INavigation Navigation
			=> Shell.Current.Navigation;

		protected void NotifyPropertyChanged(string propertyName)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
