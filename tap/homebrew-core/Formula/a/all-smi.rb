class AllSmi < Formula
  desc "GPU monitoring tool for NVIDIA/Jetson/Apple Silicon/Tenstorrent"
  homepage "https://github.com/lablup/all-smi"
  url "https://github.com/lablup/all-smi/releases/download/v0.27.1/all-smi-windows-x86_64.zip"
  sha256 "fdef49c6d305ce7aaa9c4760aac01925660656f82a182a615b6085157c1aafa0"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/lablup/all-smi/archive/refs/tags/v0.27.1.tar.gz"
  end

  def install
    bin.install "all-smi.exe"
  end

  test do
    assert_match "all-smi #{version}", shell_output("#{bin}/all-smi --version")

    system bin/"all-smi", "--config", testpath/"config.toml", "config", "init"
    assert_path_exists testpath/"config.toml"

    output = shell_output("#{bin}/all-smi --config #{testpath}/config.toml config print")
    assert_match "schema_version = 1", output
    assert_match "default_mode", output
  end
end
