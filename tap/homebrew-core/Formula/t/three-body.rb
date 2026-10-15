class ThreeBody < Formula
  desc "三体编程语言 Three Body Language written in Rust"
  homepage "https://github.com/rustq/3body-lang"
  url "https://github.com/rustq/3body-lang/releases/download/0.6.5/3body-x86_64-pc-windows-msvc.exe"
  sha256 "d8036a735d0bbd7731787948f00099aed696fb829e73196cedef43155231d981"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/rustq/3body-lang/archive/refs/tags/0.6.5.tar.gz"
  end

  def install
    bin.install "3body-x86_64-pc-windows-msvc.exe" => "3body.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/3body -V 2>&1")
  end
end
