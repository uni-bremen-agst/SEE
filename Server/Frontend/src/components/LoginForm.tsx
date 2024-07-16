import { Button, Card, CardContent, CardMedia, Divider, FormControl, FormHelperText, TextField } from "@mui/material";
import  seeLogo from "../img/see-logo.png";
import { useContext, useState } from "react";
import { AuthContext } from "../contexts/AuthContext";

function LoginForm() {
    const {setUser, axiosInstance} = useContext(AuthContext);

    const [username, setUsername] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState(false);

    async function authenticate(){
      if(!username && !password)
      {
        return;
      }
      try{
        const getUserResponse = await axiosInstance.post("/user/signin", {username: username, password: password});
        sessionStorage.setItem('username', getUserResponse.data.username);
        setUser(getUserResponse.data);
      } catch (e) {
        setError(true);

      }
    }
      

    return (
      <Card sx={{borderRadius: "25px"}}>
        <CardMedia
            sx={{objectFit: "contain", paddingBottom: "0.5em", paddingTop: "0.5em"}}
            component="img"
            height="64"
            image={seeLogo}
            title="SEE Logo"
        />
        <Divider/>
        <CardContent>
          <FormControl sx={{width: "100%"}} error={error}>
            <TextField 
              label="Benutzername" 
              type="text" 
              sx={{width: "100%", marginBottom:"1em"}} 
              InputProps={{sx: {borderRadius: "15px"}}} 
              variant="standard"
              value={username} 
              onChange={(e) => setUsername(e.target.value)} 
            />
            <TextField 
              label="Passwort" 
              type="password" 
              sx={{width: "100%", marginBottom:"1em"}}
              InputProps={{sx: {borderRadius: "15px"}}} 
              variant="standard"
              value={password} 
              onChange={(e) => setPassword(e.target.value)} 
            />
            <FormHelperText hidden={!error}>E-Mail und Passwort stimmen nicht überein, oder Benutzer existiert nicht.</FormHelperText>
            <Button variant="contained" sx={{width:"100%", borderRadius: "15px", marginTop: "1em"}} onClick={() => authenticate()}>Anmelden</Button>
          </FormControl>
        </CardContent>
      </Card>
    )
  }

  export default LoginForm;