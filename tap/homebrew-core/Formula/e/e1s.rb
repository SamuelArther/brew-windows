class E1s < Formula
  desc "TUI for managing AWS ECS, inspired by k9s"
  homepage "https://github.com/keidarcy/e1s"
  url "https://github.com/keidarcy/e1s/releases/download/v2.0.0/e1s_2.0.0_windows_amd64.tar.gz"
  sha256 "67573efb0c2c7e70b40595e0992b3e808d7a97605d751bbede824d874f6f6a56"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/keidarcy/e1s/archive/refs/tags/v2.0.0.tar.gz"
    strategy :github_releases
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "e1s.exe"
  end

  test do
    ENV["AWS_REGION"] = "us-east-1"
    ENV["AWS_ACCESS_KEY_ID"] = "test"
    ENV["AWS_SECRET_ACCESS_KEY"] = "test"

    assert_match version.to_s, shell_output("#{bin}/e1s --version")

    output = shell_output("#{bin}/e1s --json --region us-east-1 2>&1", 1)
    assert_match "e1s failed to start, please check your aws cli credential and permission", output
  end
end
