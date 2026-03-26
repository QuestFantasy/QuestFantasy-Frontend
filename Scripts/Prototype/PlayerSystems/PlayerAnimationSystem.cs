using Godot;

public class PlayerAnimationSystem
{
	private Sprite _sprite;
	private Texture[] _walkFrames;
	private int _walkFrameIndex = 1;
	private float _walkAnimationTimer;
	private bool _hasWalkFrames;

	public void Initialize(Node2D owner, string walkFrame1Path, string walkFrame2Path, string walkFrame3Path, Vector2 bodySize)
	{
		_sprite = new Sprite();
		_sprite.Centered = true;
		owner.AddChild(_sprite);

		Texture frame1 = GD.Load<Texture>(walkFrame1Path);
		Texture frame2 = GD.Load<Texture>(walkFrame2Path);
		Texture frame3 = GD.Load<Texture>(walkFrame3Path);

		_hasWalkFrames = frame1 != null && frame2 != null && frame3 != null;
		if (!_hasWalkFrames)
		{
			GD.Print("MapPlayer: walking frames not found, using fallback debug rectangle.");
			return;
		}

		_walkFrames = new[] { frame1, frame2, frame3 };
		RefreshScale(bodySize);
		ApplyCurrentFrame(1f);
	}

	public void RefreshScale(Vector2 bodySize)
	{
		if (!_hasWalkFrames || _walkFrames == null || _walkFrames.Length == 0)
		{
			return;
		}

		Vector2 textureSize = _walkFrames[0].GetSize();
		if (textureSize.x <= 0f || textureSize.y <= 0f)
		{
			return;
		}

		_sprite.Scale = new Vector2(bodySize.x / textureSize.x, bodySize.y / textureSize.y);
	}

	public void UpdateWalkAnimation(bool isMoving, float delta, float walkAnimationFps, float facingX)
	{
		if (!_hasWalkFrames)
		{
			return;
		}

		if (isMoving)
		{
			float frameDuration = 1f / Mathf.Max(1f, walkAnimationFps);
			_walkAnimationTimer += delta;
			if (_walkAnimationTimer >= frameDuration)
			{
				_walkAnimationTimer -= frameDuration;
				_walkFrameIndex = (_walkFrameIndex + 1) % _walkFrames.Length;
			}
		}
		else
		{
			_walkAnimationTimer = 0f;
			_walkFrameIndex = 1;
		}

		ApplyCurrentFrame(facingX);
	}

	public void DrawFallback(Node2D owner, Vector2 bodySize)
	{
		if (_hasWalkFrames)
		{
			return;
		}

		Rect2 rect = new Rect2(-bodySize / 2f, bodySize);
		owner.DrawRect(rect, new Color(0.95f, 0.95f, 0.98f));
		owner.DrawRect(rect.Grow(-2), new Color(0.28f, 0.28f, 0.35f), false, 2f);
	}

	private void ApplyCurrentFrame(float facingX)
	{
		if (!_hasWalkFrames)
		{
			return;
		}

		_sprite.Texture = _walkFrames[_walkFrameIndex];
		_sprite.FlipH = facingX < 0f;
	}
}
