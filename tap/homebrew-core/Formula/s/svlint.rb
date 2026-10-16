class Svlint < Formula
  desc "SystemVerilog linter"
  homepage "https://github.com/dalance/svlint"
  url "https://github.com/dalance/svlint/releases/download/v0.9.5/svlint-v0.9.5-x86_64-win.zip"
  sha256 "7ec5c8a1808ca656ff0b4a2d64e42102d696a71fd9e36b51249804b377cc22d4"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/dalance/svlint/archive/refs/tags/v0.9.5.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/bin/svlint.exe", "svlint"
  end

  test do
    (testpath/"test.sv").write <<~SYSTEMVERILOG
      module M;
      endmodule
    SYSTEMVERILOG

    assert_match(/hint\s+:\s+Begin `module` name with lowerCamelCase./, shell_output("#{bin}/svlint test.sv", 1))
  end
end
