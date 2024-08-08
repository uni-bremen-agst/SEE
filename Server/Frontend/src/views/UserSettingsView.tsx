import { Box, Button, Card, CardContent, Container, Stack, TextField, Typography } from "@mui/material";
import Header from "../components/Header";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faArrowLeft } from "@fortawesome/free-solid-svg-icons";
import { useNavigate } from "react-router";
import { useContext, useState } from "react";
import { AuthContext } from "../contexts/AuthContext";
import { enqueueSnackbar } from "notistack";
import AppUtils from "../utils/AppUtils";

function UserSettingsView() {
  const { axiosInstance, user, setUser } = useContext(AuthContext);
  const navigate = useNavigate();

  const [newUsername, setNewUsername] = useState("");
  const [changeUsernamePassword, setChangeUsernamePassword] = useState("");
  const [changeUserNameErrors, setChangeUserNameErrors] = useState<Map<string, string>>(new Map<string, string>());
  const [newPassword, setNewPassword] = useState("");
  const [newPasswordRepeat, setNewPasswordRepeat] = useState("");
  const [changePasswordPassword, setChangePasswordPassword] = useState("");
  const [changePasswordErrors, setChangePasswordErrors] = useState<Map<string, string>>(new Map<string, string>());

  async function updateUsername() {
    if (!newUsername || !changeUsernamePassword) return;
    await axiosInstance.put('/user/changeUsername', { oldUsername: user!.username, newUsername: newUsername, password: changeUsernamePassword }).then(
      (response) => {
        setUser(response.data);
        enqueueSnackbar("Username was updated.", { variant: "success" });
      }
    ).catch(
      (error) => {
        setChangeUserNameErrors(new Map(changePasswordErrors.set('changeUsernamePassword', 'Current password is not correct.')));
        AppUtils.notifyAxiosError(error, "Username Not Changed");
      }
    );
  }

  async function updatePassword() {
    if (!newPassword || !newPasswordRepeat || !changePasswordPassword) return;
    if (newPassword != newPasswordRepeat) {
      setChangePasswordErrors(new Map(changePasswordErrors.set('newPasswordRepeat', 'Passwords are not equal.')));
      return;
    }
    setChangePasswordErrors(new Map(changePasswordErrors.set('newPasswordRepeat', '')));
    await axiosInstance.put('/user/changePassword', { username: user!.username, oldPassword: changePasswordPassword, newPassword: newPassword }).then(
      () => enqueueSnackbar("Password was updated.", { variant: "success" })
    ).catch(
      (error) => {
        setChangePasswordErrors(new Map(changePasswordErrors.set('changePasswordPassword', 'Current password is not correct.')));
        AppUtils.notifyAxiosError(error, "Password Not Changed");
      }
    );
  }


  return (
    <Container sx={{ padding: "3em", height: "100vh" }}>
      <Header />
      <Typography variant="h4">
        <Box display={"inline"} sx={{ "&:hover": { cursor: "pointer" } }}>
          <FontAwesomeIcon icon={faArrowLeft} onClick={() => navigate(-1)} />&nbsp;
        </Box>
        User Settings
      </Typography>
      <Card sx={{ marginTop: "2em", borderRadius: "25px", overflow: "auto" }}>
        <CardContent>
          <Stack direction="column" spacing={2}>
            <Typography variant="h6">Change Username</Typography>
            <TextField
              label="New username"
              variant="standard" value={newUsername}
              onChange={(e) => setNewUsername(e.target.value)}
            />
            <TextField
              label="Current password"
              variant="standard"
              type="password"
              error={!!changeUserNameErrors.get("changeUsernamePassword")}
              helperText={changeUserNameErrors.get("changeUsernamePassword")}
              value={changeUsernamePassword}
              onChange={(e) => setChangeUsernamePassword(e.target.value)}
            />
            <Stack justifyContent="end" direction="row" spacing={2}>
              <Button variant="contained" sx={{ borderRadius: "25px" }} onClick={() => updateUsername()}>
                Save
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      <Card sx={{ marginTop: "2em", borderRadius: "25px", overflow: "auto" }}>
        <CardContent>
          <Stack direction="column" spacing={2}>
            <Typography variant="h6">Change Password</Typography>
            <TextField
              label="Current password"
              variant="standard"
              type="password"
              value={changePasswordPassword}
              error={!!changePasswordErrors.get("changePasswordPassword")}
              helperText={changePasswordErrors.get("changePasswordPassword")}
              onChange={(e) => setChangePasswordPassword(e.target.value)}
            />
            <TextField
              label="New password"
              variant="standard"
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
            />
            <TextField
              label="Repeat new password"
              variant="standard"
              type="password"
              value={newPasswordRepeat}
              error={!!changePasswordErrors.get("newPasswordRepeat")}
              helperText={changePasswordErrors.get("newPasswordRepeat")}
              onChange={(e) => setNewPasswordRepeat(e.target.value)}
            />
            <Stack justifyContent="end" direction="row" spacing={2}>
              <Button variant="contained" sx={{ borderRadius: "25px" }} onClick={() => updatePassword()}>
                Save
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>
    </Container>
  )
}

export default UserSettingsView;
