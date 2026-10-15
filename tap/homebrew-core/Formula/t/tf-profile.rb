class TfProfile < Formula
  desc "CLI tool to profile Terraform runs"
  homepage "https://github.com/datarootsio/tf-profile"
  url "https://github.com/datarootsio/tf-profile/releases/download/v0.5.0/tf-profile-v0.5.0-windows-amd64.zip"
  sha256 "051c32352431a36dcc9e7668531fbf442dc9323e7ffefbfb4248447802c4f271"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/datarootsio/tf-profile/archive/refs/tags/v0.5.0.tar.gz"
  end

  def install
    bin.install "tf-profile.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tf-profile version 2>&1")
  end
end
