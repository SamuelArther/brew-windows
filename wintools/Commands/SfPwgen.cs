using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace WinTools;

// sf-pwgen: password generator with the algorithms of macOS's SecurityFoundation password
// assistant (memorable, random, letters, alphanumeric, numbers), using the system CSPRNG.
// Memorable passwords join common English words with digits and punctuation, at the exact length.
static class SfPwgen
{
    [ModuleInitializer]
    internal static void Register()
    {
        Commands.Add("sf-pwgen", "Generate passwords (memorable, random, letters, alphanumeric, numbers)", Run);
    }

    static int Run(string[] args)
    {
        string algorithm = "memorable", language = "en";
        int count = 5, length = 12;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string Value()
            {
                var eq = arg.IndexOf('=');
                if (arg.StartsWith("--") && eq > 0) return arg[(eq + 1)..];
                if (++i >= args.Length) throw new UsageException($"option {arg} requires an argument");
                return args[i];
            }
            switch (arg.Split('=')[0])
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        usage: sf-pwgen [options]

                          -a, --algorithm   memorable, random, letters, alphanumeric, numbers (default: memorable)
                          -c, --count       number of passwords to generate (default: 5)
                          -l, --length      length of each password (default: 12)
                          -L, --language    language for memorable passwords (default: en)
                          -v, --version     print the version
                          -h, --help        this text
                        """);
                    return 0;
                case "-v" or "--version": Console.WriteLine($"sf-pwgen {Commands.Version} (wintools)"); return 0;
                case "-a" or "--algorithm":
                    algorithm = Value();
                    if (algorithm is not ("memorable" or "random" or "letters" or "alphanumeric" or "numbers"))
                        throw new UsageException($"unknown algorithm '{algorithm}' (memorable, random, letters, alphanumeric, numbers)");
                    break;
                case "-c" or "--count":
                    if (!int.TryParse(Value(), out count) || count < 1) throw new UsageException("count must be a positive number");
                    break;
                case "-l" or "--length":
                    if (!int.TryParse(Value(), out length) || length < 1 || length > 1024) throw new UsageException("length must be between 1 and 1024");
                    break;
                case "-L" or "--language":
                    language = Value();
                    break;
                default: throw new UsageException($"unknown option {arg}");
            }
        }
        if (algorithm == "memorable" && !language.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("sf-pwgen: only an English word list is available for memorable passwords");
            return 1;
        }
        for (var n = 0; n < count; n++)
        {
            Console.WriteLine(algorithm switch
            {
                "memorable" => Memorable(length),
                "letters" => FromSet(length, Letters),
                "alphanumeric" => FromSet(length, Letters + Digits),
                "numbers" => FromSet(length, Digits),
                _ => FromSet(length, Letters + Digits + Symbols),
            });
        }
        return 0;
    }

    const string Letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    const string Digits = "0123456789";
    const string Symbols = "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";
    const string Separators = "!#$%&*+-.:;=?@^_~";

    static string FromSet(int length, string set) =>
        string.Create(length, set, (span, s) => { for (var i = 0; i < span.Length; i++) span[i] = s[RandomNumberGenerator.GetInt32(s.Length)]; });

    // word, then 1-2 digits and a punctuation mark, then another word ... trimmed to the exact length:
    // the last gap is filled by a word of exactly that size, or digits when it is too short for one.
    internal static string Memorable(int length)
    {
        var sb = new StringBuilder();
        while (sb.Length < length)
        {
            var remaining = length - sb.Length;
            if (remaining < 3) { sb.Append(FromSet(remaining, Digits)); break; }
            if (remaining <= 8) { sb.Append(Word(remaining)); break; }
            // Leave room for a separator (2-3 chars) and at least a 3-letter word after it.
            var max = Math.Min(8, remaining - 5);
            sb.Append(Word(RandomNumberGenerator.GetInt32(3, max + 1)));
            sb.Append(FromSet(RandomNumberGenerator.GetInt32(1, 3), Digits));
            sb.Append(Separators[RandomNumberGenerator.GetInt32(Separators.Length)]);
        }
        return sb.ToString();
    }

    static string Word(int length)
    {
        var candidates = Words.Where(w => w.Length == length).ToArray();
        return candidates[RandomNumberGenerator.GetInt32(candidates.Length)];
    }

    static readonly string[] Words = """
        act add age aid air arm art ask bad bag bat bay bed bee bet big bit box boy bud bus buy cab cap car cat cow cry cup cut
        day dew dig dog dot dry due ear eat egg elf elm end era eye fan far fat fee few fig fit fix fly fog fox fun fur gap gas
        gem gum gun guy hat hay hen hip hit hop hot hub hug ice ink inn ion ivy jam jar jaw jet job jog joy key kid kit lab lap
        law leg lid lip log lot low mad map mat mix mob mud mug nap net new nut oak oar odd oil old orb owl own pad pal pan paw
        pay pea pen pet pie pig pin pit pod pot pun pup ram rat ray red rib rim rod row rub rug run sad sap saw sea set sew shy
        sip sit sky son soy spa spy sun tab tan tap tax tea ten tie tin tip toe top toy tub tug urn van vat vet war wax web wet
        wig win wit wok yak yam yes zip zoo
        able acid aged also apex arch area army atom aunt away axis baby back bake ball band bank barn base bath beam bean bear
        beat bell belt bend best bike bird bite blue boat body bold bolt bone book boot bowl brag bulb bull burn bush busy cafe
        cake calm camp cape card care cart case cash cave cell chef chin chip city clay clip club coal coat code coin cold cone
        cook cool cord corn cost crab crew crop crow cube curl dark dart dash data dawn deal dear deck deep deer desk dial dice
        dime dish dive dock dome door dove down drum duck dune dusk dust duty earl east easy echo edge epic even exam exit face
        fact fair fall fame farm fast fern film find fine fire fish flag flat flip foam fold folk font food foot fork form fort
        free frog fuel gate gear gift glow glue goal goat gold golf good gown grid grin gulf hail half hall hand harp hawk heat
        helm herb hero hill hint hive hood hook horn host hour hunt idea inch iris iron isle item jade jazz jeep joke jump jury
        keen kelp kept kind king kite knee knot lace lake lamb lamp land lane lark lava lawn leaf lens life lift lily lime line
        lion list loaf loft logo loop lord loud luck lung mail main malt mane mark mask mast maze meal melt menu mild milk mill
        mind mint mist moat mode mole moon moss moth move mule muse nail name navy neck nest news next nice node noon nose note
        oath oval oven pace page palm park part path peak pear pine pipe plan plot plum poem poet pole pond pony pool port pump
        quiz race raft rail rain ramp rank reef rice ride ring road robe rock role roof room root rope rose ruby rule safe sage
        sail salt sand seal seed ship shoe shop silk sing site size skip slab slow snow soap sock soft soil song soup spin star
        stem step suit swan tail talk tank tape team tent tide tile time tone tool tour town tree trip tube tuna tune twin unit
        vase vast verb vest view vine visa vote wade wage wall wand wave wax wear well west whip wind wing wire wise wolf wood
        wool word work yard yarn year yoga zero zone
        acorn actor adobe agent alarm album alien alley amber angle ankle apple apron arena armor arrow atlas attic award bacon
        badge bagel baker bench berry bison blade blaze blend block bloom board bonus brain brass brave bread brick bride brook
        broom brush cabin camel candy canoe cargo cedar chain chair chalk charm chart chess chief chime cider cliff clock cloud
        clown coach coast cobra coral couch crane crate creek crisp crown crumb curve daisy dance delta denim depot diary diner
        drift drill eagle earth easel elbow ember entry fable fairy feast fence ferry field flame flask fleet flint float flock
        flora flute focus forge frame frost fruit giant glass globe glove grain grape grass gravy guest guide habit harbor haven
        hazel heart hedge honey horse hotel house igloo image inlet ivory jelly jewel juice kayak kettle knife koala label lager
        lemon level light lilac linen llama lodge lotus lunar magic mango maple march marsh medal melon metal meter mirth model
        money motor mouse mural music noble north novel ocean olive onion opera orbit otter owner paint panda paper party pasta
        patch peach pearl pedal penny piano pilot pixel pizza plaid plane plant plaza poppy porch pouch prism quail quest quilt
        radar radio raven razor relay rider ridge river robin rodeo rover royal ruler salad satin sauce scale scarf scone scout
        shade shark shelf shell shore sight skate skirt slate sleet slope smile snack solar spark spice spine spoon sport squid
        stair stamp steam stone storm story stove sugar sunny swing table tango tempo thorn tiger toast token topaz torch tower
        trail train trend tribe trout truck tulip tunic ultra union urban valve vapor vault velvet venue verse video vigor villa
        vinyl viola vivid voice wafer wagon waltz watch water whale wheat wheel world yacht yield zebra
        anchor antler arcade autumn badger ballet banjo barrel basket beacon beaver bishop blazer bottle branch breeze bridge
        bronze bubble bucket bundle butter button cactus camera candle canyon carbon carpet castle cereal cherry chorus cinema
        circle citrus clever cobalt coffee comet copper cotton cradle crater crayon cruise cuckoo dagger dragon drawer dynamo
        ermine fabric falcon fennel fiddle finch flight flower forest fossil galaxy garden garlic geyser ginger glider goblet
        gravel guitar hammer harbor helmet hermit hiking hornet island jacket jaguar jersey jigsaw jungle kernel kitten ladder
        lagoon lantern laptop legend lizard locket magnet mallet marble meadow mellow mentor mirror mitten monkey mosaic muffin
        museum nectar needle nickel noodle nutmeg orange orchid oyster paddle parade parrot pastel pebble pencil pepper pickle
        pillow planet pocket pollen potato puzzle quartz rabbit radish ribbon rocket saddle salmon sandal scroll season shovel
        silver sketch sleigh socket spider spiral sponge spring sprout stable statue stream studio summer sunset tablet teapot
        temple tennis thread throne ticket timber tomato tundra turkey turtle tuxedo valley violet walnut wander window winter
        wizard yogurt zenith zipper
        acrobat almanac anthem balloon banquet biscuit blanket bobcat buffalo cabinet caravan cartoon cashew catfish cello
        chamber chapter chimney cobbler compass concert cottage cowboy cricket crystal cyclone diamond dolphin eclipse emerald
        feather festival firefly flannel freckle gazelle giraffe glacier gondola gourmet granite harvest hickory horizon
        iceberg jasmine journey juniper kingdom lantern leopard lettuce library lobster mammoth mansion meteor monarch morning
        mustang narwhal oatmeal octopus orchard origami panther papaya peacock pelican penguin pianist pilgrim pinball pioneer
        plateau popcorn pottery pumpkin pyramid quarter raccoon rainbow sapphire satchel scarlet seagull serpent shelter sparrow
        spinach station sunrise teacher thunder tornado trumpet unicorn vanilla venture village volcano weather whisker
        aardvark airplane alligator apricot backpack bluebird broccoli butterfly calendar cardinal carousel champion chipmunk
        clarinet coconut daffodil dinosaur doorbell dumpling elephant envelope evergreen fireside flamingo goldfish grizzly
        hedgehog hummingbird icicle jamboree kangaroo keyboard lemonade magnolia marathon meadowlark mountain mushroom notebook
        nugget overcoat painting pancake parsley peppermint pinecone pistachio porridge postcard pretzel quantum reindeer
        sailboat sandwich scorpion seahorse shamrock skeleton snowball squirrel starfish strawberry sunflower tapestry
        tortoise treasure umbrella vineyard waterfall woodland
        """.Split((char[])[' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries).Where(w => w.Length is >= 3 and <= 8).Distinct().ToArray();
}
