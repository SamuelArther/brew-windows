class EfmLangserver < Formula
  desc "General purpose Language Server"
  homepage "https://github.com/mattn/efm-langserver"
  url "https://github.com/mattn/efm-langserver/releases/download/v0.0.57/efm-langserver_v0.0.57_windows_amd64.zip"
  sha256 "21c346dd36e89206a8c0abea903cbc32c275068b38d7dc600bae76f53cce7e97"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mattn/efm-langserver/archive/refs/tags/v0.0.57.tar.gz"
  end

  def install
    bin.install "efm-langserver.exe"
  end

  test do
    (testpath/"config.yml").write <<~YAML
      version: 2
      root-markers:
        - ".git/"
      languages:
        python:
          - lint-command: "flake8 --stdin-display-name ${INPUT} -"
            lint-stdin: true
    YAML
    output = shell_output("#{bin}/efm-langserver -c #{testpath/"config.yml"} -d")
    assert_match "version: 2", output
    assert_match "lint-command: flake8 --stdin-display-name ${INPUT} -", output
  end
end
