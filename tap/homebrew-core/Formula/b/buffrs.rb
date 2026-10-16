class Buffrs < Formula
  desc "Modern protobuf package management"
  homepage "https://github.com/helsing-ai/buffrs"
  url "https://github.com/helsing-ai/buffrs/releases/download/v0.13.3/buffrs-v0.13.3-x86_64-pc-windows-msvc.zip"
  sha256 "25efa42db6f760c732dce3ef9a115dba3714d9a626d379fcbc6be8eb1825e15c"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/helsing-ai/buffrs/archive/refs/tags/v0.13.3.tar.gz"
  end

  def install
    bin.install "buffrs.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/buffrs --version")

    system bin/"buffrs", "init"
    assert_match "edition = \"#{version.major_minor}\"", (testpath/"Proto.toml").read

    assert_empty shell_output("#{bin}/buffrs list")
  end
end
