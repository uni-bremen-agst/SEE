import { createContext, ReactNode, useEffect, useState } from 'react'
import User from '../types/User';
import axios, { AxiosInstance } from 'axios';
import AppUtils from '../utils/AppUtils';

const axiosInstance = axios.create({
  baseURL: import.meta.env.VITE_APP_API_URL,
  timeout: 5000,
  withCredentials: true
});

interface IAuthContext {
  user: User | null;
  setUser: (newState: User | null) => void;
  axiosInstance: AxiosInstance;
}

const initialValue: IAuthContext = {
  user: null,
  setUser: () => { },
  axiosInstance: axiosInstance
}

const AuthContext = createContext<IAuthContext>(initialValue);

const AuthProvider = ({ children }: { children?: ReactNode }) => {
  const [user, setUser] = useState<User | null>(initialValue.user);

  useEffect(() => {
    if (!user && sessionStorage.getItem('username')) {
      axiosInstance.get(`/user/me`).then(
        (response) => setUser(response.data)
      ).catch(
        (error) => AppUtils.notifyAxiosError(error, "Error Fetching User Data")
      );
    }
    return () => {
    }
  }, [user])


  return (
    <AuthContext.Provider value={{ user, setUser, axiosInstance }}>
      {children}
    </AuthContext.Provider>
  )
}

export { AuthContext, AuthProvider }
