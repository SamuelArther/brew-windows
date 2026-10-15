class Typdiff < Formula
  desc "Diff tool that generates Typst documents highlighting differences between inputs"
  homepage "https://github.com/sou1118/typdiff"
  url "https://github.com/sou1118/typdiff/releases/download/v0.1.4/typdiff-x86_64-pc-windows-msvc.zip"
  sha256 "aaa6ead26cd8f3e9588cba075ddab6318c7ffaddcc9f596dcb2168c0bf9b2706"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sou1118/typdiff/archive/refs/tags/v0.1.4.tar.gz"
  end

  def install
    bin.install "typdiff.exe"
  end

  test do
    (testpath/"old.typ").write("Hello World!\n")
    (testpath/"new.typ").write("Hello Typst!\n")
    system bin/"typdiff", "old.typ", "new.typ", "-o", "diff.typ"
    expected = <<~TYPST
      #let diff-added(body) = {
        set text(fill: rgb("#0000ff"))
        underline(body)
      }
      #let diff-deleted(body) = {
        set text(fill: rgb("#cc0000"))
        strike(body)
      }

      Hello #diff-deleted[World]#diff-added[Typst]!

    TYPST
    assert_equal expected, (testpath/"diff.typ").read
  end
end
