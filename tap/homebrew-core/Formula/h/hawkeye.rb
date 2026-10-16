class Hawkeye < Formula
  desc "Simple license header checker and formatter, in multiple distribution forms"
  homepage "https://github.com/korandoru/hawkeye"
  url "https://github.com/fast/hawkeye/releases/download/v7.2.0/hawkeye-x86_64-pc-windows-msvc.zip"
  sha256 "729537a72e4bbf09ff942dc08da0c1c625d771d72a7ca27938c1cac34b4e62c9"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/korandoru/hawkeye/archive/refs/tags/v7.2.0.tar.gz"
  end

  def install
    bin.install "hawkeye.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/hawkeye --version")

    configfile = testpath/"licenserc.toml"
    configfile.write <<~TOML
      includes = ["licenserc.toml"]
    TOML

    assert_match "unknown field `includes`", shell_output("#{bin}/hawkeye format 2>&1", 2)
  end
end
