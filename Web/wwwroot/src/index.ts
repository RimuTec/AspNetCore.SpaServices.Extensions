interface User {
    id: number
    username: string
    firstName: string
    lastName: string
}

const newUser: User = {
    id: 123,
    username: 'jpreecedev',
    firstName: 'Jon',
    lastName: 'Preece'
};

console.log(newUser);
