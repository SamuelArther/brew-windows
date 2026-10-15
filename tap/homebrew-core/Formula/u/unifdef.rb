class Unifdef < Formula
  desc "Selectively process conditional C preprocessor directives"
  homepage "https://dotat.at/prog/unifdef/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-unifdef-2.12-1-any.pkg.tar.zst"
  sha256 "c9a9d49675c26d108ffcb5822c34078e24012f7dc58c5e0ba1a26a5eb4d579b8"
  license all_of: [
    "BSD-2-Clause",
    "BSD-3-Clause", # only for `unifdef.1`
  ]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://dotat.at/prog/unifdef/unifdef-2.12.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/unifdef.exe", "unifdef"
  end

  test do
    pipe_output(bin/"unifdef", "echo ''")
  end
end
