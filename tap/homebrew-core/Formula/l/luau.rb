class Luau < Formula
  desc "Fast, safe, gradually typed embeddable scripting language derived from Lua"
  homepage "https://luau.org"
  url "https://github.com/luau-lang/luau/releases/download/0.742/luau-windows.zip"
  sha256 "1114966cd8198f407069f07e8b4e6bacf2ded933e4a57d6ddc75617969278c75"
  license "MIT"
  version_scheme 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/luau-lang/luau/archive/refs/tags/0.742.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "luau.exe"
    bin.install "luau-analyze.exe"
    bin.install "luau-ast.exe"
    bin.install "luau-compile.exe"
  end

  test do
    (testpath/"test.lua").write "print ('Homebrew is awesome!')\n"
    assert_match "Homebrew is awesome!", shell_output("#{bin}/luau test.lua")
  end
end
