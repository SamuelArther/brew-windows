class Podsync < Formula
  desc "Turn YouTube or Vimeo channels, users, or playlists into podcast feeds"
  homepage "https://github.com/mxpv/podsync"
  url "https://github.com/mxpv/podsync/releases/download/v2.8.0/Podsync_2.8.0_Windows_x86_64.zip"
  sha256 "6001a0efea5da5a5117b0ac2f41fb9e26d6c839e376e384a6d20f9cc197fc940"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mxpv/podsync/archive/refs/tags/v2.8.0.tar.gz"
  end

  def install
    bin.install "podsync.exe"
  end

  test do
    port = free_port

    (testpath/"config.toml").write <<~TOML
      [server]
      port = #{port}

      [log]
      filename = "podsync.log"

      [storage]
        [storage.local]
        data_dir = "data/podsync/"

      [feeds]
        [feeds.ID1]
        url = "https://www.youtube.com/channel/UCxC5Ls6DwqV0e-CYcAKkExQ"
    TOML

    pid = spawn bin/"podsync"
    sleep 3

    Process.kill("SIGINT", pid)
    Process.wait(pid)

    assert_path_exists testpath/"podsync.log"
  end
end
