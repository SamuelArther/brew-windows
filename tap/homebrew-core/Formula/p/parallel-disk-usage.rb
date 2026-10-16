class ParallelDiskUsage < Formula
  desc "Highly parallelized, blazing fast directory tree analyzer"
  homepage "https://github.com/KSXGitHub/parallel-disk-usage"
  url "https://github.com/KSXGitHub/parallel-disk-usage/releases/download/0.24.0/pdu-x86_64-pc-windows-gnu.exe"
  sha256 "24f8d34af07bb9c3c9f57b15812382d6e788a6fc895c8a68b72f95fe19b977c5"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/KSXGitHub/parallel-disk-usage/archive/refs/tags/0.24.0.tar.gz"
  end

  def install
    bin.install "pdu-x86_64-pc-windows-gnu.exe" => "pdu.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/pdu --version")

    system bin/"pdu"

    (testpath/"test").write("test")
    system bin/"pdu", testpath/"test"
  end
end
