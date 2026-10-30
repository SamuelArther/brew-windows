class Xray < Formula
  desc "Platform for building proxies to bypass network restrictions"
  homepage "https://xtls.github.io/"
  url "https://github.com/XTLS/Xray-core/archive/refs/tags/v26.10.29.tar.gz"
  sha256 "ee22c6f21855f70a52e19bff02fa71f4b28ce071d83ff9154fafcaa0f315a642"
  license all_of: ["MPL-2.0", "CC-BY-SA-4.0"]
  head "https://github.com/XTLS/Xray-core.git", branch: "main"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "f71157623549dbfeafb9e3e39475490c4871867f6a6ea6ed57ca50540b58a176"
  end

  depends_on "go" => :build

  resource "geoip" do
    url "https://github.com/XTLS/Xray-rules-dat/releases/download/20261028222509/geoip-lite.dat"
    version "20261028222509"
    sha256 "36e0e76145dba3f13ac4bf7be0b738d7eae957835fefd2ee220e7034fe52a4d1"

    livecheck do
      url :url
    end
  end

  resource "geosite" do
    url "https://github.com/XTLS/Xray-rules-dat/releases/download/20261028222509/geosite.dat"
    version "20261028222509"
    sha256 "b8a31ce12d3ec997358bf2bde95be502df7402179d1c93d1e8b3eda6f7b08f3a"

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
