class Sha2 < Formula
  desc "Implementation of SHA-256, SHA-384, and SHA-512 hash algorithms"
  homepage "https://aarongifford.com/computers/sha.html"
  url "https://aarongifford.com/computers/sha2-1.0.1.tgz"
  mirror "https://src.fedoraproject.org/repo/pkgs/sha2/sha2-1.0.1.tgz/5c050ef4edb9d5198e7d57e759c4996f/sha2-1.0.1.tgz"
  sha256 "67bc662955c6ca2fa6a0ce372c4794ec3d0cd2c1e50b124e7a75af7e23dd1d0c"
  license "BSD-3-Clause"

  # Upstream homepage is gone
  deprecate! date: "2026-06-22", because: :repo_removed
  disable! date: "2027-06-22", because: :repo_removed

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "cbfe519b2d2f6b702e9ffc400e956f2364e0c9a17d83fbd6a3951b6233ebe347"
  end

  def install
    # Xcode 12 made -Wimplicit-function-declaration an error by default so we need to
    # disable that warning to successfully compile:
    system ENV.cc, "-o", "sha2", "-Wno-implicit-function-declaration", "sha2prog.c", "sha2.c"
    system "perl", "sha2test.pl"
    bin.install "sha2"
  end

  test do
    (testpath/"checkme.txt").write "homebrew"
    output = "12c87370d1b5472793e67682596b60efe2c6038d63d04134a1a88544509737b4"
    assert_match output, pipe_output("#{bin}/sha2 -q -256 #{testpath}/checkme.txt")
  end
end
