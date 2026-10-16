class SpotifyPlayer < Formula
  desc "Command driven spotify player"
  homepage "https://github.com/aome510/spotify-player"
  url "https://github.com/aome510/spotify-player/releases/download/v0.25.1/spotify_player-x86_64-pc-windows-msvc.zip"
  sha256 "946dfe069f3cd530548e07a86bbf71928a7983f30933e202b179a4ef3e5e16cb"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/aome510/spotify-player/archive/refs/tags/v0.25.1.tar.gz"
  end

  def install
    bin.install "spotify_player.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/spotify_player --version")

    assert_match "complete -F _spotify_player", shell_output("#{bin}/spotify_player generate bash")

    (testpath/"config/app.toml").write "client_id = 123\n"
    output = shell_output("#{bin}/spotify_player -C #{testpath}/cache -c #{testpath}/config 2>&1", 1)
    assert_match "invalid type: integer `123`, expected a string", output
  end
end
