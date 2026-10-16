class Hcledit < Formula
  desc "Command-line editor for HCL"
  homepage "https://github.com/minamijoyo/hcledit"
  url "https://github.com/minamijoyo/hcledit/releases/download/v0.2.18/hcledit_0.2.18_windows_amd64.tar.gz"
  sha256 "256595df80b616bf211eceef878cef1c443a2f87f1205ae9b22f1a7c32f247cc"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/minamijoyo/hcledit/archive/refs/tags/v0.2.18.tar.gz"
  end

  def install
    bin.install "hcledit.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/hcledit version")

    (testpath/"test.hcl").write <<~HCL
      resource "foo" "bar" {
        attr1 = "val1"
        nested {
          attr2 = "val2"
        }
      }
    HCL

    output = pipe_output("#{bin}/hcledit attribute get resource.foo.bar.attr1",
                        (testpath/"test.hcl").read, 0)
    assert_equal "\"val1\"", output.chomp
  end
end
