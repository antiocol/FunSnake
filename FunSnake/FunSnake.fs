﻿[<FunScript.JS>]
module FunSnake
 
open FunScript
open FunScript.TypeScript
 
// ------------------------------------------------------------------
// Initialization
type Direction = Left | Right | Up | Down | None
let sizeLink = 10.
let canvasSize = (300., 300.)
let snake = [(sizeLink * 5.,sizeLink * 3.0,sizeLink,sizeLink);(sizeLink * 4.,sizeLink * 3.0,sizeLink,sizeLink);(sizeLink * 3.,sizeLink * 3.0,sizeLink,sizeLink);]
let wallTop   = ([0.0..sizeLink..(fst canvasSize)] |> List.map (fun x-> (x,0.,sizeLink,sizeLink)))
let wallDown  = ([0.0..sizeLink..(fst canvasSize)] |> List.map (fun x-> (x,(snd canvasSize),sizeLink,sizeLink)))
let wallLeft  = ([0.0..sizeLink..(snd canvasSize)] |> List.map (fun x-> (0.,x,sizeLink,sizeLink)))
let wallRight = ([0.0..sizeLink..(snd canvasSize)] |> List.map (fun x-> ((fst canvasSize),x,sizeLink,sizeLink)))
let wall = wallLeft @ wallTop @ wallRight @ wallDown
let obstacles = []
let mutable direction = None   // Control snake direction
let mutable moveDone = true     // Avoid direction changes until move has done
 
 
// ------------------------------------------------------------------
// Utils functions
 
// JQuery shortcut operator selector
let jQuery(selector : string) = Globals.Dollar.Invoke selector
let (?) jq name = jq("#" + name)
 
 
// Check if element exist in a generic list
let Contains (e:'T) (el:'T list) = el |> List.exists (fun x-> x = e)
 
// Get random number with max value
let getRandomAbsolute max absolute = (Globals.Math.floor( (Globals.Math.random() * max) / absolute)) * absolute
 
// Create a gradient color for then link rectange (simulates a circular link)
let defaultGradientLink (ctx:CanvasRenderingContext2D) (link :(float*float*float*float)) colorStart colorEnd = 
        let x,y,h,w = link
        let gradient =ctx.createRadialGradient(x + (w / 2.), y + (h / 2.), 1., x+ (w /2.), y + (h / 2.), sizeLink - 4.)
        gradient.addColorStop(0.,colorStart)
        gradient.addColorStop(1.,colorEnd)
        gradient
 
// Get the color for the link (orange = alive / red = collision)
let getColorLink (ctx:CanvasRenderingContext2D) (link :(float*float*float*float)) collision aliveColor collisionColor = 
    if collision then defaultGradientLink ctx link collisionColor "white"
                 else defaultGradientLink ctx link aliveColor "white"
 
 
// ------------------------------------------------------------------
// Module program
 
// Move the snake to next position, if snake eat some food increase snake size in one link
let move xMove yMove snake food = match snake with
                                  | (x,y,h,w)::_ -> 
                                          let newHead = (x + xMove, y + yMove , h, w)
                                          if (newHead = food) then newHead :: snake
                                                              else newHead :: (snake |> List.rev |> Seq.skip 1 |> Seq.toList |> List.rev)
                                  | _ -> snake
 
// Move direction shortcuts
let moveRight snake food = move sizeLink 0. snake food
let moveLeft  snake food = move -sizeLink 0. snake food
let moveUp    snake food = move 0. -sizeLink snake   food
let moveDown  snake food = move 0. sizeLink snake  food
 
// Generate new random position avoiding positions list
let rec newPosition avoidPositions () = 
                   let randomPosition = ( (getRandomAbsolute ((fst canvasSize) - sizeLink * 2.) sizeLink) + sizeLink, (getRandomAbsolute ((snd canvasSize) - sizeLink * 2.) sizeLink) + sizeLink, sizeLink, sizeLink)
                   if avoidPositions |> Contains randomPosition then newPosition avoidPositions ()
                                                                else randomPosition

// Generate a new random food place (avoid wall & snake position)
let rec newFood snake () = newPosition snake ()

// Generate a new random obstacle (avoid snake & food position)
let rec newObstacle snake food () = newPosition (food :: snake) ()
 
// Detect snake collision (against wall or itself)
let hasCollision (snake:(float*float*float*float) List) (obstacles:(float*float*float*float) List) = wall |> Contains snake.Head || obstacles |> Contains snake.Head || snake.Tail |> Contains snake.Head 
 
// Draw snake and food in the canvas
let draw (snake:(float*float*float*float) List, food:float*float*float*float, hasCollision: bool, obstacles:(float*float*float*float) List) =
    let canvas = jQuery?canvas.[0] :?> HTMLCanvasElement
    let ctx = canvas.getContext_2d()
    ctx.clearRect(sizeLink, sizeLink, fst canvasSize - (sizeLink), snd canvasSize - (sizeLink)) // Avoid reset the wall
 
    // Draw snake head
    ctx.fillStyle <- defaultGradientLink ctx snake.Head "rgb(184,7,7)" "white"
    ctx.fillRect(snake.Head)
 
    // Draw snake tail
    snake.Tail |> List.iter (fun x-> 
                                match x with
                                | x,y,w,h -> ctx.fillStyle <- getColorLink ctx (x, y, w, h) hasCollision "orange" "red"
                                             ctx.fillRect(x, y, w, h)
                       ) |> ignore
 
    // Draw obstacles
    obstacles |> List.iter (fun x-> 
                            match x with
                            | x,y,w,h -> ctx.fillStyle <- "black"
                                         ctx.fillRect(x, y, w, h)
                    ) |> ignore

    // Draw canvas
    ctx.fillStyle <- defaultGradientLink ctx food "rgb(50,165,12)" "white"
    ctx.fillRect(food) |> ignore
 
// Draw the walls
let drawWall (wall:(float*float*float*float) List) =
    let canvas = jQuery?canvas.[0] :?> HTMLCanvasElement
    let ctx = canvas.getContext_2d()
 
    wall |> List.iter (fun x-> 
                                ctx.fillStyle <- "black"
                                match x with
                                | x,y,w,h -> ctx.fillRect(x, y, w, h)
                        ) |> ignore
 
 
let drawGameOver () =
    let canvas = jQuery?canvas.[0] :?> HTMLCanvasElement
    let ctx = canvas.getContext_2d()
    ctx.fillStyle <- "red"
    ctx.font <- "18px Segoe UI";
    let metrics = ctx.measureText("Game over!!!")
    ctx.fillText("Game Over!!!", (fst canvasSize / 2.) - (metrics.width / 2.), (snd canvasSize) / 2.) |> ignore
 
// ------------------------------------------------------------------
// Recursive update function that process the game
let rec update snake food obstacles () =
    // Snake position based on cursor direction input
    let snake = match direction with
                | Right -> moveRight snake food
                | Left  -> moveLeft  snake food
                | Up    -> moveUp    snake food
                | Down  -> moveDown  snake food
                | None  -> snake
 
    // If snake ate some food generate new random food and a new obstacle
    let food, obstacles = if (snake.Head = food) then newFood snake (), newObstacle snake food () :: obstacles
                                                 else food, obstacles
                                                 
    // Detect snake collision        
    let collision = hasCollision snake obstacles
 
    // Draw snake, food & obstacles in canvas (collision is use for paint snake in red in case of collision)
    draw (snake, food, collision, obstacles) 
 
    // Snake movement completed
    moveDone <- true
    
    // If collision, game over, otherwise, continue updating the game
    if collision then drawWall wall 
                      drawGameOver()
                      0
                 else Globals.setTimeout(update snake food obstacles, 1000. / 10.) |> ignore
                      1
 
// ------------------------------------------------------------------
// Main function
let main() = 
    // Capture arrows keys to move the snake
    Globals.window.addEventListener_keydown(fun e -> 
                                                    Globals.console.log(e.keyCode)
                                                    if moveDone then
                                                            if e.keyCode = 65. && (direction = None || direction = Up || direction = Down) then direction <- Left
                                                            if e.keyCode = 87. && (direction = None || direction = Right || direction = Left) then direction <- Up
                                                            if e.keyCode = 68. && (direction = None || direction = Up || direction = Down) then direction <- Right
                                                            if e.keyCode = 83. && (direction = None || direction = Right || direction = Left) then direction <- Down
                                                            moveDone <- false
                                                    :> obj
                                           )
    // Draw the walls only once
    drawWall wall 
 
    // Start the game with basic snake and ramdom food
    update snake (newFood snake ()) obstacles () |> ignore

