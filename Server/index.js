const express = require('express')
const mysql = require('mysql')
const db = require('./config/database.js')
const connection = mysql.createConnection(db)

const app = express()

app.set('port', process.env.PORT || 3000)

app.use(express.json())

// ===========================================    API DEFINITION    ===========================================
app.get('/', (req, res) => {
  res.send('Root')
})

// =====================================    Users     =====================================
app.get('/users', (req, res) => {
  connection.query('SELECT * FROM Crescendor.users;', (error, rows) => {
    if (error) throw error
    console.log('User info is: ', rows)
    res.send(rows)
  })
})

// =====================================    Record    =====================================
app.get('/record', (req, res) => {
  connection.query('SELECT * from Crescendor.record;', (error, rows) => {
    if (error) throw error
    console.log('Record info is: ', rows)
    res.send(rows)
  })
})

// getscore API
app.get('/record/getscore/:user_id/:music_id', (req, res) => {
  const user_id = req.params.user_id
  const music_id = Number(req.params.music_id)

  connection.query("SELECT score from Crescendor.record where (user_id = ? && music_id = ?);", [user_id, music_id], (error, rows) => {
    if (error) throw error
    console.log('getscore \n user: %s \n music: %d \n', user_id, music_id)
    console.log(rows)
    res.send(rows)
  })
}) 

// addscore API
app.post('/record/addscore/:user_id/:music_id', (req, res) => {
  const user_id = req.params.user_id
  const music_id = Number(req.params.music_id)
  const { score, date, midi } = req.body

  connection.query("INSERT INTO Crescendor.record SET user_id = ?, music_id = ? score = ?, date = ?, midi = ?;", [user_id, music_id, score, date, midi], (error, rows) => {
    if (error) throw error
    console.log('addscore \n user: %s \n music: %d \n', user_id, music_id)
    res.send(rows)
  })
})

// setscore API
app.put('/record/setscore/:user_id/:music_id', (req, res) => {
  const user_id = req.params.user_id
  const music_id = Number(req.params.music_id)
  const { score, date, midi } = req.body

  connection.query("UPDATE Crescendor.record SET score = ?, date = ?, midi = ? where (user_id = ? && music_id = ?);", [score, date, midi,user_id, music_id], (error, rows) => {
    if (error) throw error
    console.log('setscore \n user: %s \n music: %d \n', user_id, music_id)
    console.log(rows)
    res.send(rows)
  })
}) 

// =====================================    Practice    =====================================
app.get('/practice', (req, res) => {
  connection.query('SELECT * from Crescendor.practice;', (error, rows) => {
    if (error) throw error
    console.log('Practice info is: ', rows)
    res.send(rows)
  })
})

// =====================================    Music   =====================================
app.get('/music', (req, res) => {
  connection.query('SELECT * from Crescendor.music;', (error, rows) => {
    if (error) throw error
    console.log('Music info is: ', rows)
    res.send(rows)
  })
})


app.listen(app.get('port'), () => {
  console.log('Express server listening on port ' + app.get('port'))
})