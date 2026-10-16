class Leetup < Formula
  desc "Command-line tool to solve Leetcode problems"
  homepage "https://github.com/dragfire/leetup"
  url "https://github.com/dragfire/leetup/releases/download/v1.2.5/leetup-x86_64-pc-windows-msvc.zip"
  sha256 "e85b6c168f25dbc2fefa0c0925e5a74daf6322ded781e5edd7ca31bcc3e4aa6d"
  license "MIT"
  revision 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/dragfire/leetup/archive/refs/tags/v1.2.5.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "leetup.exe"
  end

  test do
    assert_match <<~EOS, shell_output("#{bin}/leetup user --logout")
      User not logged in!
      User logged out!
    EOS

    assert_match version.to_s, shell_output("#{bin}/leetup --version")
  end
end
