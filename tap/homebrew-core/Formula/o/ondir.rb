class Ondir < Formula
  desc "Automatically execute scripts as you traverse directories"
  homepage "https://swapoff.org/ondir.html"
  url "https://swapoff.org/files/ondir/ondir-0.2.4.tar.gz"
  sha256 "52921cdcf02273e0d47cc6172df6a0d2c56980d724568276acb0591e0bda343a"
  license "GPL-2.0-or-later"
  head "https://github.com/alecthomas/ondir.git", branch: "master"

  # Homepage doesn't list current versions, `/files/ondir/` isn't checkable
  # (403 Forbidden), and the GitHub repository hasn't been updated since 2014
  # (and doesn't tag versions anyway).
  livecheck do
    skip "Not actively developed or maintained"
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "01198d59e16029187c41df281894df8cb5081da6b616f262e12b21131b9cb11c"
  end

  deny_network_access!

  def install
    system "make"
    system "make", "PREFIX=#{prefix}", "install"
  end

  test do
    (testpath/".ondirrc").write <<~EOS
      enter /projects/(proj[0-9]+)
      \techo "entering $1 in $ONDIRWD"

      leave /projects/proj1
      \techo "leaving $ONDIRWD"
    EOS

    ENV["HOME"] = testpath
    output = shell_output("#{bin}/ondir /projects/proj1/src /projects/proj2/lib")
    assert_match 'echo "leaving /projects/proj1"', output
    assert_match 'echo "entering proj2 in /projects/proj2"', output
    refute_match "entering proj1", output

    assert_match "not absolute", shell_output("#{bin}/ondir relative/path 2>&1", 1)
  end
end
