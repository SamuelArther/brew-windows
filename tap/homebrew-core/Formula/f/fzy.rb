class Fzy < Formula
  desc "Fast, simple fuzzy text selector with an advanced scoring algorithm"
  homepage "https://github.com/jhawthorn/fzy"
  url "https://repo.msys2.org/msys/x86_64/fzy-1.1-1-x86_64.pkg.tar.zst"
  sha256 "3087db81fa2482e3d976b52102cc3a8af97a457c918c236076676aa5b378084d"
  license "MIT"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://github.com/jhawthorn/fzy/archive/refs/tags/1.1.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/fzy.exe", "fzy"
  end

  test do
    assert_equal "foo", pipe_output("#{bin}/fzy -e foo", "bar\nfoo\nqux").chomp
  end
end
