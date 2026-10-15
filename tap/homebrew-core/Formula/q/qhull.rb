class Qhull < Formula
  desc "Computes convex hulls in n dimensions"
  homepage "http://www.qhull.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-qhull-2020.2-4-any.pkg.tar.zst"
  sha256 "0828ce9810d008cd05e573964669d738ed74befddb8eae2619de5f563f6c24d9"
  license "Qhull"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "http://www.qhull.org/download/"
    regex(/href=.*?qhull[._-][^"' >]+?[._-]src[^>]*?\.t[^>]+?>[^<]*Qhull v?(\d+(?:\.\d+)*)/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/qconvex.exe", "qconvex"
    msys2_bin "ucrt64/bin/qdelaunay.exe", "qdelaunay"
    msys2_bin "ucrt64/bin/qhalf.exe", "qhalf"
    msys2_bin "ucrt64/bin/qhull.exe", "qhull"
    msys2_bin "ucrt64/bin/qvoronoi.exe", "qvoronoi"
    msys2_bin "ucrt64/bin/rbox.exe", "rbox"
  end

  test do
    input = shell_output("#{bin}/rbox c D2")
    output = pipe_output("#{bin}/qconvex s n 2>&1", input, 0)
    assert_match "Number of facets: 4", output
  end
end
