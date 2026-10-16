class Selene < Formula
  desc "Blazing-fast modern Lua linter"
  homepage "https://kampfkarren.github.io/selene"
  url "https://github.com/Kampfkarren/selene/releases/download/0.32.0/selene-0.32.0-windows.zip"
  sha256 "c8b693de81d0ddc11490d8e1170dd98d775c2b21431144bd8286f738ef416109"
  license "MPL-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Kampfkarren/selene/archive/refs/tags/0.32.0.tar.gz"
  end

  def install
    bin.install "selene.exe"
  end

  test do
    (testpath/"selene.toml").write("std = \"lua52\"")
    (testpath/"test.lua").write("print(1 / 0)")
    assert_match "warning[divide_by_zero]", shell_output("#{bin}/selene #{testpath}/test.lua", 1)
  end
end
