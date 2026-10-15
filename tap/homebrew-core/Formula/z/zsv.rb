class Zsv < Formula
  desc "Tabular data swiss-army knife CLI"
  homepage "https://github.com/liquidaty/zsv"
  url "https://github.com/liquidaty/zsv/releases/download/v1.4.4/zsv-1.4.4-amd64-windows-mingw.tar.gz"
  sha256 "e948aac22146b9d781ca9d407cbd3bad6932b5ca3ac8205f402fc73b8a7f9ada"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/liquidaty/zsv/archive/refs/tags/v1.4.4.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/bin/zsv.exe", "zsv"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/zsv version")

    input = <<~CSV
      a,b,c
      1,2,3
    CSV
    assert_equal "1", pipe_output("#{bin}/zsv count", input).strip
  end
end
