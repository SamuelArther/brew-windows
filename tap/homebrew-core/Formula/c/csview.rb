class Csview < Formula
  desc "High performance csv viewer for cli"
  homepage "https://github.com/wfxr/csview"
  url "https://github.com/wfxr/csview/releases/download/v1.3.4/csview-v1.3.4-x86_64-pc-windows-msvc.zip"
  sha256 "fc490a108ad7ecaa30950b7c79f9b0c7f5426e1507c9e993efba2581be7e5ec8"
  license any_of: ["MIT", "Apache-2.0"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/wfxr/csview/archive/refs/tags/v1.3.4.tar.gz"
  end

  def install
    bin.install "csview.exe"
  end

  test do
    (testpath/"test.csv").write("a,b,c\n1,2,3")
    assert_equal <<~EOS, shell_output("#{bin}/csview #{testpath}/test.csv")
      ┌───┬───┬───┐
      │ a │ b │ c │
      ├───┼───┼───┤
      │ 1 │ 2 │ 3 │
      └───┴───┴───┘
    EOS
  end
end
