class Blogc < Formula
  desc "Blog compiler with template engine and markup language"
  homepage "https://blogc.rgm.io/"
  url "https://github.com/blogc/blogc/releases/download/v0.20.1/blogc-win64-0.20.1.zip"
  sha256 "ddd97c94a5cbfe1fe7d4125779814d2eb7dcc8e0aed68131336977a0310c0636"
  license "BSD-3-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/blogc/blogc/releases/download/v0.20.1/blogc-0.20.1.tar.xz"
  end

  def install
    bin.install "blogc.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/blogc -version 2>&1")
  end
end
