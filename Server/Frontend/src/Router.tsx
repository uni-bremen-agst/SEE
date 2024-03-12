import { Route, Routes } from 'react-router'
import LoginView from './views/LoginView'
import HomeView from './views/HomeView'
import ServerView from './views/ServerView'
import CreateServerView from './views/CreateServerView'
import SettingsView from './views/SettingsView'
import PersonalSettingsView from './views/PersonalSettingsView'
import { BrowserRouter } from 'react-router-dom'
import { useContext } from 'react'
import { AuthContext } from './contexts/AuthContext'

function Router() {
  const { user } = useContext(AuthContext);

  return (
    <BrowserRouter>
      {user ? <PrivateRoutes/> : <PublicRoutes/>}
    </BrowserRouter>
  )
}

function PublicRoutes() {
  return(
    <Routes>
        <Route path="*" element={<LoginView/>}/> 
    </Routes> 
  )
}

function PrivateRoutes() {
  return(
    <Routes>
        <Route path="/" element={<HomeView/>}/> 
        <Route path="/server" element={<ServerView/>}/> 
        <Route path="/createServer" element={<CreateServerView/>}/> 
        <Route path="/settings" element={<SettingsView/>}/> 
        <Route path="/personalSettings" element={<PersonalSettingsView/>}/> 
      </Routes> 
  )
}

export default Router
