using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

//adapted from this tutorial: https://lodev.org/cgtutor/raycasting.html

namespace _3d
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private KeyboardState keyboardState;

        Texture2D t; //base for the line texture

        Vector2 size = new Vector2(320, 240);
        readonly float scaleFactor = 3f;
        Matrix scaleTransormation;

        Vector2 position;
        Vector2 direction;
        Vector2 plane;

        int[] lineHeights;
        Color[] lineColors;

        int[,] worldMap = new int [24, 24] {
            {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
            {1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
            {1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
            {1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
            {1,0,0,0,0,0,2,2,2,2,2,0,0,0,0,3,0,3,0,3,0,0,0,1},
            {1,0,0,0,0,0,2,0,0,0,2,0,0,0,0,0,0,0,0,0,0,0,0,1},
            {1,0,0,0,0,0,2,0,0,0,2,0,0,0,0,3,0,0,0,3,0,0,0,1},
            {1,0,0,0,0,0,2,0,0,0,2,0,0,0,0,0,0,0,0,0,0,0,0,1},
            {1,0,0,0,0,0,2,2,0,2,2,0,0,0,0,3,0,3,0,3,0,0,0,1},
            {1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
            {1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
            {1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
            {1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
            {1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
            {1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
            {1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
            {1,4,4,4,4,4,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
            {1,4,0,4,0,0,0,0,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
            {1,4,0,0,0,0,5,0,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
            {1,4,0,4,0,0,0,0,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
            {1,4,0,4,4,4,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
            {1,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
            {1,4,4,4,4,4,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
            {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1}
        };

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            // store the scale transformation that will be used for all drawing
            Vector3 scaleVector3 = new Vector3(scaleFactor, scaleFactor, 1);
            scaleTransormation = Matrix.CreateScale(scaleVector3);

            // graphics init
            graphics.PreferredBackBufferWidth = (int)(size.X * scaleFactor);
            graphics.PreferredBackBufferHeight = (int)(size.Y * scaleFactor);
            graphics.ApplyChanges();

            position = new Vector2(22, 12);
            direction = new Vector2(-1, 0);
            plane = new Vector2(0, 0.66f);

            // init empty line heights and colors
            lineHeights = new int[(int) size.X];
            lineColors = new Color[(int) size.X];

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            //one pixel texture for line drawing
            t = new Texture2D(GraphicsDevice, 1, 1);
            t.SetData<Color>(
                new Color[] { Color.White });// fill the texture with white
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            keyboardState = Keyboard.GetState();

            //loop through width
            for (int x = 0; x < size.X; x++)
            {
                //calculate ray position and direction
                float cameraX = 2 * x / size.X - 1; //x-coordinate in camera space
                float rayDirX = direction.X + plane.X * cameraX;
                float rayDirY = direction.Y + plane.Y * cameraX;

                //which box of the map we're in
                int mapX = (int) position.X;
                int mapY = (int) position.Y;

                //length of ray from current position to next x or y-side
                double sideDistX;
                double sideDistY;

                //length of ray from one x or y-side to next x or y-side
                double deltaDistX = Math.Abs(1 / rayDirX);
                double deltaDistY = Math.Abs(1 / rayDirY);
                double perpWallDist;

                //what direction to step in x or y-direction (either +1 or -1)
                int stepX;
                int stepY;

                int hit = 0; //was there a wall hit?
                int side = 0; //was a NS or a EW wall hit?

                //calculate step and initial sideDist
                if (rayDirX < 0)
                {
                    stepX = -1;
                    sideDistX = (position.X - mapX) * deltaDistX;
                }
                else
                {
                    stepX = 1;
                    sideDistX = (mapX + 1.0 - position.X) * deltaDistX;
                }
                if (rayDirY < 0)
                {
                    stepY = -1;
                    sideDistY = (position.Y - mapY) * deltaDistY;
                }
                else
                {
                    stepY = 1;
                    sideDistY = (mapY + 1.0 - position.Y) * deltaDistY;
                }

                //perform DDA
                while (hit == 0)
                {
                    //jump to next map square, OR in x-direction, OR in y-direction
                    if (sideDistX < sideDistY)
                    {
                        sideDistX += deltaDistX;
                        mapX += stepX;
                        side = 0;
                    }
                    else
                    {
                        sideDistY += deltaDistY;
                        mapY += stepY;
                        side = 1;
                    }

                    //Check if ray has hit a wall
                    if (worldMap[mapX, mapY] > 0)
                    {
                        hit = 1;
                    }
                }

                //Calculate distance projected on camera direction (Euclidean distance will give fisheye effect!)
                if (side == 0)
                {
                    perpWallDist = (mapX - position.X + (1 - stepX) / 2) / rayDirX;
                } 
                else
                {
                    perpWallDist = (mapY - position.Y + (1 - stepY) / 2) / rayDirY;
                }

                //Calculate height of line to draw on screen
                lineHeights[x] = (int)(size.Y / perpWallDist);
                Color color = worldMap[mapX, mapY] switch
                {
                    1 => Color.Red,
                    2 => Color.Green,
                    3 => Color.Blue,
                    4 => Color.White,
                    5 => Color.Teal,
                    _ => Color.Yellow,
                };

                //give x and y sides different brightness
                if (side == 1)
                {
                    color = Color.Lerp(color, Color.Black, 0.5f);
                }
                lineColors[x] = color;
            }

            //process movement
            //speed modifiers
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            float moveSpeed = dt * 5.0f; //the constant value is in squares/second
            float rotSpeed = dt * 3.0f; //the constant value is in radians/second


            var posX = position.X;
            var posY = position.Y;
            var dirX = direction.X;
            var dirY = direction.Y;

            //move forward if no wall in front of you
            if (keyboardState.IsKeyDown(Keys.W))
            {
                if (worldMap[(int) (posX + dirX * moveSpeed),(int) posY] == 0) position.X += (float) (dirX * moveSpeed);
                if (worldMap[(int) posX,(int) (posY + dirY * moveSpeed)] == 0) position.Y += (float) (dirY * moveSpeed);
            }
            //move backwards if no wall behind you
            if (keyboardState.IsKeyDown(Keys.S))
            {
                if (worldMap[(int)(posX - dirX * moveSpeed),(int)(posY)] == 0) position.X -= (float) (dirX * moveSpeed);
                if (worldMap[(int)(posX),(int)(posY - dirY * moveSpeed)] == 0) position.Y -= (float) (dirY * moveSpeed);
            }
            //rotate to the right
            if (keyboardState.IsKeyDown(Keys.D))
            {
                //both camera direction and camera plane must be rotated
                float oldDirX = dirX;
                direction.X = (float) (dirX * Math.Cos(-rotSpeed) - dirY * Math.Sin(-rotSpeed));
                direction.Y = (float)(oldDirX * Math.Sin(-rotSpeed) + dirY * Math.Cos(-rotSpeed));
                float oldPlaneX = plane.X;
                plane.X = (float)(plane.X * Math.Cos(-rotSpeed) - plane.Y * Math.Sin(-rotSpeed));
                plane.Y = (float)(oldPlaneX * Math.Sin(-rotSpeed) + plane.Y * Math.Cos(-rotSpeed));
            }
            //rotate to the left
            if (keyboardState.IsKeyDown(Keys.A))
            {
                //both camera direction and camera plane must be rotated
                float oldDirX = dirX;
                direction.X = (float)(dirX * Math.Cos(rotSpeed) - dirY * Math.Sin(rotSpeed));
                direction.Y = (float)(oldDirX * Math.Sin(rotSpeed) + dirY * Math.Cos(rotSpeed));
                float oldPlaneX = plane.X;
                plane.X = (float)(plane.X * Math.Cos(rotSpeed) - plane.Y * Math.Sin(rotSpeed));
                plane.Y = (float)(oldPlaneX * Math.Sin(rotSpeed) + plane.Y * Math.Cos(rotSpeed));
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                null,
                SamplerState.PointClamp,
                null,
                null,
                null,
                scaleTransormation
            );

            for (int x = 0; x < size.X; x++)
            {
                var lineHeight = lineHeights[x];
                var h = (int) size.Y;

                //calculate lowest and highest pixel to fill in current stripe
                int drawStart = -lineHeight / 2 + h / 2;
                if (drawStart < 0) drawStart = 0;
                int drawEnd = lineHeight / 2 + h / 2;
                if (drawEnd >= h) drawEnd = h - 1;

                Vector2 start = new Vector2(x, drawStart);
                Vector2 end = new Vector2(x, drawEnd);

                DrawLine(spriteBatch, start, end, lineColors[x]);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }

        void DrawLine(SpriteBatch sb, Vector2 start, Vector2 end, Color color)
        {
            Vector2 edge = end - start;
            // calculate angle to rotate line
            float angle =
                (float)Math.Atan2(edge.Y, edge.X);

            sb.Draw(t,
                new Rectangle(// rectangle defines shape of line and position of start of line
                    (int)start.X,
                    (int)start.Y,
                    (int)edge.Length(), //sb will strech the texture to fill this rectangle
                    1), //width of line, change this to make thicker line
                null,
                color, //colour of line
                angle,     //angle of line (calulated above)
                new Vector2(0, 0), // point in line about which to rotate
                SpriteEffects.None,
                0);
        }
    }
}
