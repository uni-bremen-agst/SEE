import { AxiosError } from 'axios';
import { closeSnackbar, enqueueSnackbar, SnackbarKey } from 'notistack';

class AppUtils {
  private static connectionLostSnackbarId: SnackbarKey | null = null;

  static notifyAxiosError(error: AxiosError, title: string = "Request Error") {
    if (!error) return;
    let message = error.message;
    if (error.response?.data) {
      const data = error.response.data as { message: string };
      if (data.message) message = data.message;
    }
    enqueueSnackbar(<div><b>{title}</b><br /><span>{message}</span></div>, { variant: "error", style: { whiteSpace: 'pre-line' } });
  }

  static notifyOffline(offline: boolean = true) {
    if (offline && AppUtils.connectionLostSnackbarId == null) {
      AppUtils.connectionLostSnackbarId = enqueueSnackbar('Connection lost.', {
        variant: 'error',
        persist: true
      });
    }
    if (!offline && AppUtils.connectionLostSnackbarId != null) {
      closeSnackbar(AppUtils.connectionLostSnackbarId);
      AppUtils.connectionLostSnackbarId = null;
    }
  }

  static notifyOnline = () => AppUtils.notifyOffline(false);
}


export default AppUtils;
