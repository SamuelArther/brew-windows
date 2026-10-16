class Risor < Formula
  desc "Fast and flexible scripting for Go developers and DevOps"
  homepage "https://risor.io/"
  url "https://github.com/deepnoodle-ai/risor/releases/download/v2.2.0/risor_Windows_x86_64.zip"
  sha256 "9e07e0e27a972229be247696acbde2fafd764a33b4d738a78e6c810883beaa50"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/deepnoodle-ai/risor/archive/refs/tags/v2.2.0.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "risor.exe"
  end

  test do
    output = shell_output("#{bin}/risor -c \"len([1, 2, 3])\"")
    assert_equal "3\n", output
    assert_match version.to_s, shell_output("#{bin}/risor version")

    assert_match "_risor_completion", shell_output("#{bin}/risor completion bash")
    assert_match "unsupported shell: powershell",
                 shell_output("#{bin}/risor completion powershell 2>&1", 1)
  end
end
