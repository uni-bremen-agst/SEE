import { Box, Button, Card, CardContent, Container, IconButton, List, ListItem, ListItemText, Modal, Stack, TextField, Typography } from "@mui/material";
import Header from "../components/Header";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faArrowLeft, faCrown, faRepeat, faUserMinus } from "@fortawesome/free-solid-svg-icons";
import { grey, yellow } from "@mui/material/colors";
import { useContext, useEffect, useState } from "react";
import User from "../types/User";
import { Navigate, useNavigate } from "react-router";
import { AuthContext } from "../contexts/AuthContext";
import AppUtils from "../utils/AppUtils";

const modalStyle = {
  position: 'absolute',
  top: '50%',
  left: '50%',
  transform: 'translate(-50%, -50%)',
  maxWidth: "1200px",
  minWidth: "400px",
  bgcolor: 'background.paper',
  borderRadius: "25px",
  boxShadow: 24,
  p: 4,
};

function generateRandomPassword() {
  const password = [];
  for (let i = 0; i < 16; i++) {
    password.push(String.fromCharCode(Math.random() * 86 + 40));
  }
  return password.join("");
}

function SettingsView() {
  const { user, axiosInstance } = useContext(AuthContext);

  const navigate = useNavigate();

  const [addUserModalOpen, setAddUserModalOpen] = useState(false);
  const [removeUserModalOpen, setRemoveUserModalOpen] = useState(false);
  const [promoteDemoteUserModalOpen, setPromoteDemoteUserModalOpen] = useState(false);
  const [users, setUsers] = useState<User[]>([]);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const [addUserUsername, setAddUserUsername] = useState("");
  const [addUserPassword, setAddUserPassword] = useState("");

  async function addUser() {
    if (!addUserUsername || !addUserPassword) return;
    await axiosInstance.post('/user/create', { username: addUserUsername, password: addUserPassword, role: 'ROLE_USER' }).then(() => {
      setAddUserModalOpen(false);
      setAddUserUsername("");
      return axiosInstance.get("/user/all");
    }).then(
      (response) => setUsers(response.data)
    ).catch(
      (error) => AppUtils.notifyAxiosError(error, "Error Adding User")
    ).finally(
      () => setAddUserPassword("")
    );
  }

  async function removeUser() {
    if (!selectedUser) return;
    await axiosInstance.delete('/user/delete', { params: { username: selectedUser.username } }).then(
      () => {
        setRemoveUserModalOpen(false);
        return axiosInstance.get("/user/all");
      }
    ).then(
      (response) => setUsers(response.data)
    ).catch(
      (error) => AppUtils.notifyAxiosError(error, "Error Removing User")
    );
  }

  async function promoteUser() {
    if (!selectedUser) return;
    await axiosInstance.post('/user/addRoleToUser', {}, { params: { username: selectedUser.username, role: "ROLE_ADMIN" } }).then(
      () => {
        setPromoteDemoteUserModalOpen(false);
        return axiosInstance.get("/user/all");
      }
    ).then(
      (response) => setUsers(response.data)
    ).catch(
      (error) => AppUtils.notifyAxiosError(error, "Error Promoting User")
    );

  }

  async function demoteUser() {
    if (!selectedUser) return;
    await axiosInstance.delete('/user/removeRoleFromUser', { params: { username: selectedUser.username, role: "ROLE_ADMIN" } }).then(
      () => {
        setPromoteDemoteUserModalOpen(false);
        return axiosInstance.get("/user/all");
      }
    ).then(
      (response) => setUsers(response.data)
    ).catch(
      (error) => AppUtils.notifyAxiosError(error, "Error Demoting User")
    );
  }

  async function refreshData() {
    await axiosInstance.get("/user/all").then(
      (response) => {
        setUsers(response.data);
        AppUtils.notifyOnline();
      }
    ).catch(
      () => AppUtils.notifyOffline()
    );
  }

  useEffect(() => {
    refreshData();
    const refreshInterval = setInterval(() => refreshData(), 10000);
    return () => {
      clearInterval(refreshInterval);
    }
  }, [])

  if (!user?.roles.some((item) => item.name == "ROLE_ADMIN")) {
    return <Navigate to="/" />
  }
  else return (
    <Container sx={{ padding: "3em" }}>
      <Modal
        open={addUserModalOpen}
        onClose={() => setAddUserModalOpen(false)}
        aria-labelledby="add-user-modal-title">
        <Box sx={modalStyle}>
          <Typography id="add-user-modal-title" variant="h6">
            Create User
          </Typography>
          <Stack direction="column" spacing={2}>
            <TextField
              label="Username"
              variant="standard"
              value={addUserUsername}
              onChange={(e) => setAddUserUsername(e.target.value)}
            />
            <TextField
              label="Password"
              variant="standard"
              value={addUserPassword}
              onChange={(e) => setAddUserPassword(e.target.value)}
              InputProps={{ endAdornment: <IconButton size="small" onClick={() => setAddUserPassword(generateRandomPassword())}><FontAwesomeIcon icon={faRepeat} /></IconButton> }}
            />
            <Stack justifyContent="end" direction="row" spacing={2}>
              <Button variant="contained" color="secondary" sx={{ borderRadius: "25px" }} onClick={() => { setAddUserModalOpen(false); setAddUserUsername(""); setAddUserPassword(""); }}>
                Cancel
              </Button>
              <Button variant="contained" sx={{ borderRadius: "25px" }} onClick={() => addUser()}>
                Create
              </Button>
            </Stack>
          </Stack>

        </Box>
      </Modal>
      <Modal
        open={removeUserModalOpen}
        onClose={() => setRemoveUserModalOpen(false)}
        aria-labelledby="remove-user-modal-title"
        aria-describedby="remove-user-modal-description">
        <Box sx={modalStyle}>
          <Typography id="remove-user-modal-title" variant="h6">
            Delete User
          </Typography>
          <Typography id="remove-user-modal-description" sx={{ marginTop: "2em" }}>
            Are you sure that you want to delete user <b>{selectedUser ? selectedUser.username : ""}</b>?
          </Typography>
          <Stack justifyContent="end" direction="row" spacing={2} sx={{ marginTop: "2em" }}>
            <Button variant="contained" color="secondary" sx={{ borderRadius: "25px" }} onClick={() => setRemoveUserModalOpen(false)}>
              Cancel
            </Button>
            <Button variant="contained" color="error" sx={{ borderRadius: "25px" }} onClick={() => removeUser()}>
              Delete
            </Button>
          </Stack>
        </Box>
      </Modal>
      <Modal
        open={promoteDemoteUserModalOpen}
        onClose={() => setPromoteDemoteUserModalOpen(false)}
        aria-labelledby="promote-demote-user-modal-title"
        aria-describedby="promote-demote-modal-description">
        <Box sx={modalStyle}>
          <Typography id="promote-demote-modal-title" variant="h6">
            {selectedUser?.roles.some((item) => item.name == "ROLE_ADMIN") ? "Demote" : "Promote"} User
          </Typography>
          <Typography id="promote-demote-modal-description" sx={{ marginTop: "2em" }}>
            Are you sure that you want to {selectedUser?.roles.some((item) => item.name == "ROLE_ADMIN") ? "demote" : "promote"} <b>{selectedUser ? selectedUser.username : ""}</b> to {selectedUser?.roles.some((item) => item.name == "ROLE_ADMIN") ? "user" : "admin"}?
          </Typography>
          <Stack justifyContent="end" direction="row" spacing={2} sx={{ marginTop: "2em" }}>
            <Button variant="contained" color="secondary" sx={{ borderRadius: "25px" }} onClick={() => setPromoteDemoteUserModalOpen(false)}>
              Cancel
            </Button>
            <Button variant="contained" sx={{ borderRadius: "25px" }} onClick={() => { if (selectedUser?.roles.some((item) => item.name == "ROLE_ADMIN")) { demoteUser() } else { promoteUser() } }}>
              {selectedUser?.roles.some((item) => item.name == "ROLE_ADMIN") ? "Demote" : "Promote"}
            </Button>
          </Stack>
        </Box>
      </Modal>

      <Header />
      <Typography variant="h4">
        <Box display={"inline"} sx={{ "&:hover": { cursor: "pointer" } }}>
          <FontAwesomeIcon icon={faArrowLeft} onClick={() => navigate(-1)} />&nbsp;
        </Box>
        Settings
      </Typography>
      <Card sx={{ marginTop: "2em", borderRadius: "25px", overflow: "auto" }}>
        <CardContent>
          <Stack direction="column" spacing={2} height={"100%"}>
            <Typography variant="h6">User Management</Typography>
            <Card sx={{ borderRadius: "25px", backgroundColor: grey[200], flexGrow: 1, overflow: "auto", minHeight: "100px", maxHeight: "100%" }}>
              <CardContent>
                <List>
                  {
                    users && users.length > 0 ?
                      users.map(
                        (listUser) =>
                          <ListItem key={listUser.username} sx={{ backgroundColor: "white", borderRadius: "25px", marginBottom: "1em" }}
                            secondaryAction={
                              <>
                                <IconButton onClick={() => { setSelectedUser(listUser); setPromoteDemoteUserModalOpen(true); }}
                                  disabled={listUser?.roles.some((item) => item.name == "ROLE_ADMIN") && (users.filter((u) => u?.roles.some((item) => item.name == "ROLE_ADMIN"))).length < 2 || listUser.username == user.username}>
                                  <FontAwesomeIcon icon={faCrown} color={listUser?.roles.some((item) => item.name == "ROLE_ADMIN") ? yellow[600] : undefined} />
                                </IconButton>
                                <IconButton onClick={() => { setSelectedUser(listUser); setRemoveUserModalOpen(true); }}
                                  disabled={users.length < 2 || (listUser?.roles.some((item) => item.name == "ROLE_ADMIN"))}>
                                  <FontAwesomeIcon icon={faUserMinus} />
                                </IconButton>
                              </>
                            }>
                            <ListItemText>
                              <Typography variant="subtitle2">{listUser.username}</Typography>
                            </ListItemText>
                          </ListItem>
                      )
                      : <></>
                  }
                </List>
              </CardContent>
            </Card>
            <Stack justifyContent="end" direction="row" spacing={2}>
              <Button variant="contained" sx={{ borderRadius: "25px" }} onClick={() => setAddUserModalOpen(true)}>
                New User
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>
    </Container>
  )
}

export default SettingsView;
