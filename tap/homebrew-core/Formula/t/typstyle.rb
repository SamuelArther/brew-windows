class Typstyle < Formula
  desc "Beautiful and reliable typst code formatter"
  homepage "https://typstyle-rs.github.io/typstyle/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-typstyle-0.15.2-1-any.pkg.tar.zst"
  sha256 "548d537a8f0f5aa466b0f567eff2bc11692037a7c9df26da83dd8b63bb40db14"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/typstyle-rs/typstyle/archive/refs/tags/v0.15.2.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/typstyle.exe", "typstyle"
  end

  test do
    (testpath/"Hello.typ").write("Hello World!")
    system bin/"typstyle", "Hello.typ"

    assert_match version.to_s, shell_output("#{bin}/typstyle --version")
  end
end
