class Xray < Formula
  desc "Platform for building proxies to bypass network restrictions"
  homepage "https://xtls.github.io/"
  url "https://github.com/XTLS/Xray-core/archive/refs/tags/v26.10.31.tar.gz"
  sha256 "be44f95d497ad4ba6e9d1dcfbbd71e8e51f812aff077d4c15c9947eff5e3046a"
  license all_of: ["MPL-2.0", "CC-BY-SA-4.0"]
  head "https://github.com/XTLS/Xray-core.git", branch: "main"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "c773b797d90d4c80aaf4580838d54c8998ac6304801d517b932c7c4a9f657e73"
  end

  depends_on "go" => :build

  resource "geoip" do
    url "https://github.com/XTLS/Xray-rules-dat/releases/download/20261031001536/geoip-lite.dat"
    version "20261031001536"
    sha256 "ce90dc02a81528109c99d769e1f8fe3071ded6b02a574f31e62d8f4eee312563"

    livecheck do
      url :url
    end
  end

  resource "geosite" do
    url "https://github.com/XTLS/Xray-rules-dat/releases/download/20261031001536/geosite.dat"
    version "20261031001536"
    sha256 "9ee94acb06e2f795831fb1589c0dc7af9242b0fb8d9196afd981b915b1097ed3"

    livecheck do
      url :url
    end
  end

  resource "example_config" do
    # borrow v2ray example config
    url "https://raw.githubusercontent.com/v2fly/v2ray-core/v5.55.0/release/config/config.json"
    sha256 "15a66415d72df4cd77fcd037121f36604db244dcfa7d45d82a0c33de065c6a87"

    livecheck do
      url "https://github.com/v2fly/v2ray-core.git"
    end
  end

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    execpath = libexec/name
    system "go", "build", *std_go_args(output: execpath, ldflags: "-buildid="), "./main"
    (bin/"xray").write_env_script execpath,
      XRAY_LOCATION_ASSET: "${XRAY_LOCATION_ASSET:-#{pkgshare}}"

    resource("geoip").stage do
      pkgshare.install "geoip-lite.dat" => "geoip.dat"
    end
    resource("geosite").stage do
      pkgshare.install "geosite.dat"
    end
    pkgetc.install resource("example_config")
  end

  def caveats
    <<~EOS
      An example config is installed to #{etc}/xray/config.json
    EOS
  end

  service do
    run [opt_bin/"xray", "run", "--config", "#{etc}/xray/config.json"]
    run_type :immediate
    keep_alive true
  end

  test do
    (testpath/"config.json").write <<~JSON
      {
        "log": {
          "access": "#{testpath}/log"
        },
        "outbounds": [
          {
            "protocol": "freedom",
            "tag": "direct"
          }
        ],
        "routing": {
          "rules": [
            {
              "ip": [
                "geoip:private"
              ],
              "outboundTag": "direct",
              "type": "field"
            },
            {
              "domains": [
                "geosite:private"
              ],
              "outboundTag": "direct",
              "type": "field"
            }
          ]
        }
      }
    JSON
    output = shell_output "#{bin}/xray -c #{testpath}/config.json -test"

    assert_match "Configuration OK", output
    assert_path_exists testpath/"log"
  end
end
