class Yajl < Formula
  desc "Yet Another JSON Library"
  homepage "https://lloyd.github.io/yajl/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-yajl-2.1.0-3-any.pkg.tar.zst"
  sha256 "a43570865e93cd9dd3fdb152234364ea3c8ed4b63e0abb42a7667239d70371b0"
  license "ISC"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/lloyd/yajl/archive/refs/tags/2.1.0.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/json_reformat.exe", "json_reformat"
    msys2_bin "ucrt64/bin/json_verify.exe", "json_verify"
  end

  test do
    output = pipe_output("#{bin}/json_verify", "[0,1,2,3]").strip
    assert_equal "JSON is valid", output
  end
end
