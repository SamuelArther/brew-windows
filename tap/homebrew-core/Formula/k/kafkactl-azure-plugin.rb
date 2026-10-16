class KafkactlAzurePlugin < Formula
  desc "Azure Plugin for kafkactl"
  homepage "https://deviceinsight.github.io/kafkactl/"
  url "https://github.com/deviceinsight/kafkactl-plugins/releases/download/v1.3.1/kafkactl-azure-plugin_1.3.1_windows_amd64.zip"
  sha256 "d4bb684a5ae579c42e4c71b2687afe55591735c5617cb4ef6817d94648ebce06"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/deviceinsight/kafkactl-plugins/archive/refs/tags/v1.3.1.tar.gz"
  end

  def install
    bin.install "kafkactl-azure-plugin.exe"
  end

  test do
    assert_path_exists bin/"kafkactl-azure-plugin.exe"
  end
end
