class Roswell < Formula
  desc "Lisp installer and launcher for major environments"
  homepage "https://github.com/roswell/roswell"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-roswell-26.10.118-1-any.pkg.tar.zst"
  sha256 "a4b9ab415d6d76ba46af720a46cfcbabc2ff6a1e1a83c3f150b133c090a54101"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/roswell/roswell/archive/refs/tags/v26.10.118.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/ros.exe", "ros"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ros --version 2>&1")
  end
end
