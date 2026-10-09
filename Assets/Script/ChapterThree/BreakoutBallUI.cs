using UnityEngine;

public class BreakoutBallUI : MonoBehaviour
{
    private Minigame12_Breakout game;
    private RectTransform rect;
    private Vector2 direction;
    private float speed;
    private bool active = false;

    public Vector2 GetDirection()
    {
        return direction;
    }

    public Vector2 GetAnchoredPosition()
    {
        return rect != null
            ? rect.anchoredPosition
            : Vector2.zero;
    }

    public Vector2 GetBallSize()
    {
        return rect != null
            ? rect.rect.size
            : Vector2.zero;
    }

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public void Launch(
        Minigame12_Breakout owner,
        Vector2 startDirection,
        float startSpeed)
    {
        game = owner;
        direction = startDirection.normalized;
        speed = startSpeed;
        active = true;
    }

    private void Update()
    {
        if (!active || game == null)
            return;

        if (!game.IsGameRunning())
            return;

        rect.anchoredPosition +=
            direction * speed * Time.deltaTime;

        CheckWallBounce();

        if (!active)
            return;

        CheckPaddleBounce();

        if (!active)
            return;

        CheckBrickCollision();
    }

    private void CheckWallBounce()
    {
        RectTransform playArea = game.GetPlayArea();

        if (playArea == null)
            return;

        Vector2 position = rect.anchoredPosition;

        float halfWidth = rect.rect.width * 0.5f;
        float halfHeight = rect.rect.height * 0.5f;

        float left = playArea.rect.xMin + halfWidth;
        float right = playArea.rect.xMax - halfWidth;
        float top = playArea.rect.yMax - halfHeight;
        float bottom = playArea.rect.yMin + halfHeight;

        if (position.x <= left)
        {
            position.x = left;
            direction.x = Mathf.Abs(direction.x);
        }

        if (position.x >= right)
        {
            position.x = right;
            direction.x = -Mathf.Abs(direction.x);
        }

        if (position.y >= top)
        {
            position.y = top;
            direction.y = -Mathf.Abs(direction.y);
        }

        if (position.y <= bottom)
        {
            active = false;
            game.OnBallLost(this);
            return;
        }

        rect.anchoredPosition = position;
    }

    private void CheckPaddleBounce()
    {
        RectTransform paddle = game.GetPaddle();

        if (paddle == null || direction.y >= 0f)
            return;

        if (!Intersects(rect, paddle))
            return;

        float offset =
            (rect.anchoredPosition.x -
             paddle.anchoredPosition.x) /
            (paddle.rect.width * 0.5f);

        offset = Mathf.Clamp(offset, -1f, 1f);

        direction = new Vector2(offset, 1f).normalized;

        Vector2 position = rect.anchoredPosition;

        position.y =
            paddle.anchoredPosition.y +
            paddle.rect.height * 0.5f +
            rect.rect.height * 0.5f +
            2f;

        rect.anchoredPosition = position;
    }

    private void CheckBrickCollision()
    {
        foreach (RectTransform brick in game.normalBricks)
        {
            if (brick == null ||
                !brick.gameObject.activeSelf)
            {
                continue;
            }

            if (!Intersects(rect, brick))
                continue;

            BounceFromRect(brick);

            game.OnHitNormalBrick(brick, this);

            return;
        }

        RectTransform startBrick = game.startButtonBrick;

        if (startBrick != null &&
            startBrick.gameObject.activeSelf &&
            Intersects(rect, startBrick))
        {
            active = false;
            game.OnHitStartButton();
        }
    }

    private void BounceFromRect(RectTransform other)
    {
        Rect ballRect = GetWorldRect(rect);
        Rect otherRect = GetWorldRect(other);

        float overlapLeft =
            ballRect.xMax - otherRect.xMin;

        float overlapRight =
            otherRect.xMax - ballRect.xMin;

        float overlapBottom =
            ballRect.yMax - otherRect.yMin;

        float overlapTop =
            otherRect.yMax - ballRect.yMin;

        float overlapX =
            Mathf.Min(overlapLeft, overlapRight);

        float overlapY =
            Mathf.Min(overlapBottom, overlapTop);

        if (overlapX < overlapY)
        {
            direction.x *= -1f;
        }
        else
        {
            direction.y *= -1f;
        }

        direction.Normalize();
    }

    private bool Intersects(
        RectTransform a,
        RectTransform b)
    {
        return GetWorldRect(a).Overlaps(GetWorldRect(b));
    }

    private Rect GetWorldRect(RectTransform target)
    {
        Vector3[] corners = new Vector3[4];

        target.GetWorldCorners(corners);

        Vector3 bottomLeft = corners[0];
        Vector3 topRight = corners[2];

        return new Rect(
            bottomLeft.x,
            bottomLeft.y,
            topRight.x - bottomLeft.x,
            topRight.y - bottomLeft.y
        );
    }
}