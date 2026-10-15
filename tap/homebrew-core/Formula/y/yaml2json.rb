class Yaml2json < Formula
  desc "Command-line tool convert from YAML to JSON"
  homepage "https://github.com/bronze1man/yaml2json"
  url "https://github.com/bronze1man/yaml2json/releases/download/v1.3.5/yaml2json_windows_amd64.exe"
  sha256 "97fc3291d385bf77880f8d583ec77eef20f6dddfa57d0bb1009b58f85e81abd1"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/bronze1man/yaml2json/archive/refs/tags/v1.3.5.tar.gz"
  end

  def install
    bin.install "yaml2json_windows_amd64.exe" => "yaml2json.exe"
  end

  test do
    assert_path_exists bin/"yaml2json.exe"
  end
end
