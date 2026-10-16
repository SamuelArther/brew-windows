class BitbucketCli < Formula
  desc "CLI for Bitbucket Cloud and Data Center"
  homepage "https://github.com/avivsinai/bitbucket-cli"
  url "https://github.com/avivsinai/bitbucket-cli/releases/download/v0.34.0/bkt_0.34.0_windows_x86_64.zip"
  sha256 "07be0736c5ed9fb2f9eb5b5d9ef057db5314239075924af6e634c9c855eddf0c"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/avivsinai/bitbucket-cli/archive/refs/tags/v0.34.0.tar.gz"
  end

  conflicts_with "bkt", because: "both install `bkt` binaries"

  def install
    bin.install "bkt.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/bkt --version")
    assert_match "No contexts configured", shell_output("#{bin}/bkt context list")
    assert_match "no active context", shell_output("#{bin}/bkt repo list 2>&1", 1)
  end
end
