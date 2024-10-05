package de.unibremen.swt.see.manager.controller;

import com.fasterxml.jackson.databind.ObjectMapper;
import de.unibremen.swt.see.manager.service.UserService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.test.web.servlet.MockMvc;

public class UserControllerTest {

    @Autowired
    private MockMvc mockMvc;

    @MockBean
    private UserService userService;

    @Autowired
    private ObjectMapper objectMapper;

//    public UserControllerTest() {}
//
//    @BeforeAll
//    public static void setUpClass() {}
//
//    @AfterAll
//    public static void tearDownClass() {}
//
//    @BeforeEach
//    public void setUp() {}
//
//    @AfterEach
//    public void tearDown() {}

//    /**
//     * Test of {@code getUser} method, of class {@code UserController}.
//     */
//    @Test
//    public void testGetUser() {
//        System.out.println("getAll");
//
//        User user1 = new User("Alfred", "passw0rd?");
//        User user2 = new User("Berta", "p4ssword!");
//
//        when(userService.getByUsername("")).thenReturn(Optional.of(user1));
//
//        mockMvc.perform(get("/api/products/1"))
//            .andExpect(status().isOk())
//
//        UserDetails userDetails = null;
//        UserController instance = null;
//        ResponseEntity expResult = null;
//
//        ResponseEntity result = instance.getUser(userDetails);
//        assertEquals(expResult, result);
//        // TODO review the generated test code and remove the default call to fail.
//        fail("The test case is a prototype.");
//    }

//    /**
//     * Test of getUsers method, of class UserController.
//     */
//    @Test
//    public void testGetUsers() {
//        System.out.println("getUsers");
//        UserController instance = null;
//        ResponseEntity expResult = null;
//        ResponseEntity result = instance.getUsers();
//        assertEquals(expResult, result);
//        // TODO review the generated test code and remove the default call to fail.
//        fail("The test case is a prototype.");
//    }
//
//    /**
//     * Test of createUser method, of class UserController.
//     */
//    @Test
//    public void testCreateUser() {
//        System.out.println("createUser");
//        SignupRequest signupRequest = null;
//        UserController instance = null;
//        ResponseEntity expResult = null;
//        ResponseEntity result = instance.createUser(signupRequest);
//        assertEquals(expResult, result);
//        // TODO review the generated test code and remove the default call to fail.
//        fail("The test case is a prototype.");
//    }
//
//    /**
//     * Test of addRoleToUser method, of class UserController.
//     */
//    @Test
//    public void testAddRoleToUser() {
//        System.out.println("addRoleToUser");
//        String username = "";
//        RoleType role = null;
//        UserController instance = null;
//        ResponseEntity expResult = null;
//        ResponseEntity result = instance.addRoleToUser(username, role);
//        assertEquals(expResult, result);
//        // TODO review the generated test code and remove the default call to fail.
//        fail("The test case is a prototype.");
//    }
//
//    /**
//     * Test of removeRoleToUSer method, of class UserController.
//     */
//    @Test
//    public void testRemoveRoleToUSer() {
//        System.out.println("removeRoleToUSer");
//        String username = "";
//        RoleType role = null;
//        UserController instance = null;
//        ResponseEntity expResult = null;
//        ResponseEntity result = instance.removeRoleToUSer(username, role);
//        assertEquals(expResult, result);
//        // TODO review the generated test code and remove the default call to fail.
//        fail("The test case is a prototype.");
//    }
//
//    /**
//     * Test of deleteUser method, of class UserController.
//     */
//    @Test
//    public void testDeleteUser() {
//        System.out.println("deleteUser");
//        String username = "";
//        UserController instance = null;
//        ResponseEntity expResult = null;
//        ResponseEntity result = instance.deleteUser(username);
//        assertEquals(expResult, result);
//        // TODO review the generated test code and remove the default call to fail.
//        fail("The test case is a prototype.");
//    }
//
//    /**
//     * Test of changeUsername method, of class UserController.
//     */
//    @Test
//    public void testChangeUsername() {
//        System.out.println("changeUsername");
//        UserDetails oldUserDetails = null;
//        ChangeUsernameRequest changeUsernameRequest = null;
//        UserController instance = null;
//        ResponseEntity expResult = null;
//        ResponseEntity result = instance.changeUsername(oldUserDetails, changeUsernameRequest);
//        assertEquals(expResult, result);
//        // TODO review the generated test code and remove the default call to fail.
//        fail("The test case is a prototype.");
//    }
//
//    /**
//     * Test of changePassword method, of class UserController.
//     */
//    @Test
//    public void testChangePassword() {
//        System.out.println("changePassword");
//        UserDetails userDetails = null;
//        ChangePasswordRequest changePasswordRequest = null;
//        UserController instance = null;
//        ResponseEntity expResult = null;
//        ResponseEntity result = instance.changePassword(userDetails, changePasswordRequest);
//        assertEquals(expResult, result);
//        // TODO review the generated test code and remove the default call to fail.
//        fail("The test case is a prototype.");
//    }
//
//    /**
//     * Test of authenticateUser method, of class UserController.
//     */
//    @Test
//    public void testAuthenticateUser() {
//        System.out.println("authenticateUser");
//        LoginRequest loginRequest = null;
//        UserController instance = null;
//        ResponseEntity expResult = null;
//        ResponseEntity result = instance.authenticateUser(loginRequest);
//        assertEquals(expResult, result);
//        // TODO review the generated test code and remove the default call to fail.
//        fail("The test case is a prototype.");
//    }
//
//    /**
//     * Test of logoutUser method, of class UserController.
//     */
//    @Test
//    public void testLogoutUser() {
//        System.out.println("logoutUser");
//        UserController instance = null;
//        ResponseEntity expResult = null;
//        ResponseEntity result = instance.logoutUser();
//        assertEquals(expResult, result);
//        // TODO review the generated test code and remove the default call to fail.
//        fail("The test case is a prototype.");
//    }

}
