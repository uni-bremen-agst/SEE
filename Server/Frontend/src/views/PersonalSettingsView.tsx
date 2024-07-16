import {Alert, Box, Button, Card, CardContent, Container, Snackbar, Stack, TextField, Typography } from "@mui/material";
import Header from "../components/Header";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faArrowLeft } from "@fortawesome/free-solid-svg-icons";
import { useNavigate } from "react-router";
import { useContext, useState } from "react";
import { AuthContext } from "../contexts/AuthContext";

function PersonalSettingsView() {
    const {axiosInstance, user, setUser} = useContext(AuthContext);
    const navigate = useNavigate();

    const [showChangedPassword, setShowChangedPassword] = useState(false);
    const [showChangedUsername, setShowChangedUsername] = useState(false);

    const [newUsername, setNewUsername] = useState("");
    const [changeUsernamePassword, setChangeUsernamePassword] = useState("");
    const [changeUserNameErrors, setChangeUserNameErrors] = useState<Map<string,string>>(new Map<string, string>());
    const [newPassword, setNewPassword] = useState("");
    const [newPasswordRepeat, setNewPasswordRepeat] = useState("");
    const [changePasswordPassword, setChangePasswordPassword] = useState("");
    const [changePasswordErrors, setChangePasswordErrors] = useState<Map<string,string>>(new Map<string, string>());

    async function updateUsername(){
      if(!newUsername || !changeUsernamePassword){
        return;
      } 
      try{
        const changeUserNameResponse = await axiosInstance.put('/user/changeUsername', {oldUsername: user!.username, newUsername: newUsername, password: changeUsernamePassword})
        if(changeUserNameResponse){
          setUser(changeUserNameResponse.data);
          setShowChangedUsername(true);
        }
      } catch {
        setChangeUserNameErrors(new Map(changePasswordErrors.set('changeUsernamePassword', 'Angegebenes Passwort stimmt nicht mit gespeichertem überein.')));
      }
    }

    async function updatePassword(){
      if(!newPassword || !newPasswordRepeat || !changePasswordPassword){
        return;
      }
      if(newPassword != newPasswordRepeat){
        setChangePasswordErrors(new Map(changePasswordErrors.set('newPasswordRepeat', 'Passwörter stimmen nicht überein.')));
      } else {
        setChangePasswordErrors(new Map(changePasswordErrors.set('newPasswordRepeat', '')));
        try{
          const changePasswordResponse = await axiosInstance.put('/user/changePassword', {username: user!.username, oldPassword: changePasswordPassword, newPassword: newPassword})
          if(changePasswordResponse){
            setShowChangedPassword(true);
          }
        } catch {
          setChangePasswordErrors(new Map(changePasswordErrors.set('changePasswordPassword', 'Angegebenes Passwort stimmt nicht mit gespeichertem überein.')));
        }
      }
    }


    return (
      <Container sx={{padding: "3em", height:"100vh"}}>
        <Snackbar open={showChangedPassword} autoHideDuration={5000} onClose={() => setShowChangedPassword(false)}>
          <Alert onClose={() => setShowChangedPassword(false)} severity="success" sx={{width: "100%", borderRadius: "25px"}}>
            Passwort aktualisiert.
          </Alert>
        </Snackbar>
        <Snackbar open={showChangedUsername} autoHideDuration={5000} onClose={() => setShowChangedUsername(false)}>
          <Alert onClose={() => setShowChangedUsername(false)} severity="success" sx={{width: "100%", borderRadius: "25px"}}>
            Benutzername aktualisiert.
          </Alert>
        </Snackbar>
        <Header/>
        <Card sx={{marginTop: "2em", borderRadius: "25px", height: "calc(100% - 100px)", overflow: "auto"}}>
          <CardContent sx={{height: "calc(100% - 3em)"}}>
            <Stack direction="column" spacing={2} height={"100%"}>
              <Typography variant="h4"><Box display={"inline"} sx={{"&:hover" : {cursor: "pointer"}}}><FontAwesomeIcon icon={faArrowLeft} onClick={() => navigate(-1)}/></Box> Benutzereinstellungen</Typography>              
              <Typography variant="h6">Benutzername ändern:</Typography>
              <TextField 
                label="Neuer Benutzername" 
                variant="standard" value={newUsername} 
                onChange={(e) => setNewUsername(e.target.value)}
              />
              <TextField 
                label="Aktuelles Passwort" 
                variant="standard" 
                type="password"
                error={!!changeUserNameErrors.get("changeUsernamePassword")}
                helperText={changeUserNameErrors.get("changeUsernamePassword")}
                value={changeUsernamePassword} 
                onChange={(e) => setChangeUsernamePassword(e.target.value)}
              />
              <Stack justifyContent="end" direction="row" spacing={2}>
                <Button variant="contained" sx={{borderRadius:"25px"}} onClick={() => updateUsername()}>
                    Speichern
                </Button>
              </Stack>
              <Typography variant="h6">Passwort ändern:</Typography>
              <TextField 
                label="Neues Passwort" 
                variant="standard" 
                type="password"
                value={newPassword} 
                onChange={(e) => setNewPassword(e.target.value)}
              />
              <TextField 
                label="Neues Passwort wiederholen" 
                variant="standard" 
                type="password"
                value={newPasswordRepeat} 
                error={!!changePasswordErrors.get("newPasswordRepeat")}
                helperText={changePasswordErrors.get("newPasswordRepeat")}
                onChange={(e) => setNewPasswordRepeat(e.target.value)}
              />
              <TextField 
                label="Aktuelles Passwort" 
                variant="standard" 
                type="password"
                value={changePasswordPassword} 
                error={!!changePasswordErrors.get("changePasswordPassword")}
                helperText={changePasswordErrors.get("changePasswordPassword")}
                onChange={(e) => setChangePasswordPassword(e.target.value)}
              />
              <Stack justifyContent="end" direction="row" spacing={2}>
                <Button variant="contained" sx={{borderRadius:"25px"}} onClick={() => updatePassword()}>
                    Speichern
                </Button>
              </Stack>
            </Stack>
          </CardContent>
        </Card>
      </Container>
    )
  }

  export default PersonalSettingsView;