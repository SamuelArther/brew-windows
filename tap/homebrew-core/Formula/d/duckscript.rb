class Duckscript < Formula
  desc "Simple, extendable and embeddable scripting language"
  homepage "https://sagiegurari.github.io/duckscript"
  url "https://github.com/sagiegurari/duckscript/releases/download/0.11.1/duckscript-v0.11.1-x86_64-pc-windows-msvc.zip"
  sha256 "581db06e418d45c386a3796010216f8b39a756abaf09521d06a5ad09da5bdc00"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sagiegurari/duckscript/archive/refs/tags/0.11.1.tar.gz"
  end

  def install
    bin.install "duck.exe"
  end

  test do
    (testpath/"hello.ds").write <<~EOS
      out = set "Hello World"
      echo The out variable holds the value: ${out}
    EOS
    output = shell_output("#{bin}/duck hello.ds")
    assert_match "The out variable holds the value: Hello World", output
  end
end
