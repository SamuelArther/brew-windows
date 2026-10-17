class PodmanTui < Formula
  desc "Podman Terminal User Interface"
  homepage "https://github.com/containers/podman-tui"
  url "https://github.com/containers/podman-tui/archive/refs/tags/v2.0.0.tar.gz"
  sha256 "32c8ffced718cda2d5b4bcedddb71299aaa035f035de6886100ab0f6469ca3dd"
  license "Apache-2.0"
  head "https://github.com/containers/podman-tui.git", branch: "main"

  no_autobump! because: :bumped_by_upstream

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "c10663a556ea30b1efe1ea6589af5bc5b23d2c2e8d231f74e72fb2e8f21f7e1c"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ENV["CGO_ENABLED"] = "0"
    tags = "exclude_graphdriver_btrfs containers_image_openpgp remote"
    system "go", "build", *std_go_args(tags:)
  end

  test do
    require "pty"
    ENV["TERM"] = "xterm"

    PTY.spawn(bin/"podman-tui") do |r, w, _pid|
      sleep 4
      w.write "\cC"
      begin
        output = r.read
        assert_match "Connection:", output
        assert_match "SYSTEM CONNECTIONS[0]", output
      rescue Errno::EIO
        # GNU/Linux raises EIO when read is done on closed pty
      end
    end

    assert_match "podman-tui v#{version}", shell_output("#{bin}/podman-tui version")
  end
end
