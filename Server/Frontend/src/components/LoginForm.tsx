import { Button, Card, CardContent, CardMedia, Divider, FormControl, FormHelperText, TextField } from "@mui/material";
import seeLogo from "../img/see-logo.png";
import { useContext, useRef, useState } from "react";
import { AuthContext } from "../contexts/AuthContext";
import AppUtils from "../utils/AppUtils";

function LoginForm() {
  const { setUser, axiosInstance } = useContext(AuthContext);

  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState(false);

  const passwordInputRef = useRef<HTMLInputElement>(null);

  async function authenticate() {
    if (!username && !password) {
      return;
    }

    await axiosInstance.post("/user/signin", { username: username, password: password }).then(
      (response) => {
        sessionStorage.setItem('username', response.data.username);
        setUser(response.data);
      }
    ).catch(
      (error) => {
        setError(true);
        AppUtils.notifyAxiosError(error, "Error During Sign-In");
        if (passwordInputRef.current) {
          passwordInputRef.current.select();
        }
      }
    );
  }

  const handleUsernameKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      if (passwordInputRef.current) {
        passwordInputRef.current.focus();
        passwordInputRef.current.select();
      }
    }
  };

  const handlePasswordKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      authenticate();
    }
  };

  return (
    <Card sx={{ borderRadius: "25px" }}>
      <CardMedia
        sx={{ objectFit: "contain", paddingBottom: "0.5em", paddingTop: "0.5em" }}
        component="img"
        height="64"
        image={seeLogo}
        title="SEE Logo"
      />
      <Divider />
      <CardContent>
        <FormControl sx={{ width: "100%" }} error={error}>
          <TextField
            label="Username"
            type="text"
            sx={{ width: "100%", marginBottom: "1em" }}
            InputProps={{ sx: { borderRadius: "15px" } }}
            variant="standard"
            value={username}
            onChange={(e) => { setError(false); setUsername(e.target.value) }}
            onKeyDown={handleUsernameKeyDown}
            autoFocus
          />
          <TextField
            label="Password"
            type="password"
            sx={{ width: "100%", marginBottom: "1em" }}
            InputProps={{ sx: { borderRadius: "15px" } }}
            variant="standard"
            value={password}
            onChange={(e) => { setError(false); setPassword(e.target.value) }}
            onKeyDown={handlePasswordKeyDown}
            inputRef={passwordInputRef}
          />
          <FormHelperText hidden={!error}>Authentication failed.</FormHelperText>
          <Button
            type="submit"
            variant="contained"
            sx={{ width: "100%", borderRadius: "15px", marginTop: "1em" }}
            onClick={() => authenticate()}>
            Log In
          </Button>
        </FormControl>
      </CardContent>
    </Card>
  )
}

export default LoginForm;
