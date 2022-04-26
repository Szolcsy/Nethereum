﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.ABI.Decoders;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts.IntegrationTests.FiltersEvents;
using Nethereum.Contracts.Standards.ERC20;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.Contracts.Standards.ERC20.TokenList;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Services;
using Nethereum.XUnitEthereumClients;
using Newtonsoft.Json;
using Xunit;

namespace Nethereum.Contracts.IntegrationTests.SmartContracts.Standards
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class ERC20Tests
    {

        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public ERC20Tests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void ShouldGetTheDaiFromMainnet()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var contractHandler = web3.Eth.GetContractHandler("0x89d24A6b4CcB1B6fAA2625fE562bDD9a23260359");
            var stringBytes32Decoder = new StringBytes32Decoder();
            var symbol = await contractHandler.QueryRawAsync<SymbolFunction, StringBytes32Decoder, string>();
            var token = await contractHandler.QueryRawAsync<NameFunction, StringBytes32Decoder, string>();

        }

        [Fact]
        public async void ShouldReturnData()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var deploymentHandler = web3.Eth.GetContractDeploymentHandler<EIP20Deployment>();
            var receipt = await deploymentHandler.SendRequestAndWaitForReceiptAsync(new EIP20Deployment()
            {
                DecimalUnits = 18,
                InitialAmount = BigInteger.Parse("10000000000000000000000000"),
                TokenSymbol = "XST",
                TokenName = "XomeStandardToken"
            });

            var contractHandler = web3.Eth.GetContractHandler(receipt.ContractAddress);
            var symbol = await contractHandler.QueryRawAsync<SymbolFunction, StringBytes32Decoder, string>();
            var token = await contractHandler.QueryRawAsync<NameFunction, StringBytes32Decoder, string>();

            Assert.Equal("XST", symbol);
            Assert.Equal("XomeStandardToken", token);
        }

        [Fact]
        public async void Test()
        {
            var addressOwner = EthereumClientIntegrationFixture.AccountAddress;
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            ulong totalSupply = 1000000;
            var newAddress = "0x12890d2cce102216644c59daE5baed380d84830e";

            var deploymentContract = new EIP20Deployment()
            {
                InitialAmount = totalSupply,
                TokenName = "TestToken",
                TokenSymbol = "TST"
            };

            var deploymentHandler = web3.Eth.GetContractDeploymentHandler<EIP20Deployment>();
            var receipt = await deploymentHandler.SendRequestAndWaitForReceiptAsync(deploymentContract);
            var tokenService = web3.Eth.ERC20.GetContractService(receipt.ContractAddress);
            
            var transfersEvent = tokenService.GetTransferEvent();

            var totalSupplyDeployed = await tokenService.TotalSupplyQueryAsync();
            Assert.Equal(totalSupply, totalSupplyDeployed);

            var tokenName = await tokenService.NameQueryAsync();
            Assert.Equal("TestToken", tokenName);

            var tokenSymbol = await tokenService.SymbolQueryAsync();
            Assert.Equal("TST", tokenSymbol);

            var ownerBalance = await tokenService.BalanceOfQueryAsync(addressOwner);
            Assert.Equal(totalSupply, ownerBalance);

            var transferReceipt =
                await tokenService.TransferRequestAndWaitForReceiptAsync(newAddress, 1000);

            ownerBalance = await tokenService.BalanceOfQueryAsync(addressOwner);
            Assert.Equal(totalSupply - 1000, ownerBalance);

            var newAddressBalance = await tokenService.BalanceOfQueryAsync(newAddress);
            Assert.Equal(1000, newAddressBalance);

            var allTransfersFilter =
                await transfersEvent.CreateFilterAsync(new BlockParameter(transferReceipt.BlockNumber));
            var eventLogsAll = await transfersEvent.GetAllChangesAsync(allTransfersFilter);
            Assert.Single(eventLogsAll);
            var transferLog = eventLogsAll.First();
            Assert.Equal(transferLog.Log.TransactionIndex.HexValue, transferReceipt.TransactionIndex.HexValue);
            Assert.Equal(transferLog.Log.BlockNumber.HexValue, transferReceipt.BlockNumber.HexValue);
            Assert.Equal(transferLog.Event.To.ToLower(), newAddress.ToLower());
            Assert.Equal(transferLog.Event.Value, (ulong)1000);

            var approveTransactionReceipt = await tokenService.ApproveRequestAndWaitForReceiptAsync(newAddress, 1000);
            var allowanceAmount = await tokenService.AllowanceQueryAsync(addressOwner, newAddress);
            Assert.Equal(1000, allowanceAmount);
        }

        public class EIP20Deployment : ContractDeploymentMessage
        {
#if !BYTECODELITE
            public static string BYTECODE =
                "608060405234801561001057600080fd5b506040516107843803806107848339810160409081528151602080840151838501516060860151336000908152808552959095208490556002849055908501805193959094919391019161006991600391860190610096565b506004805460ff191660ff8416179055805161008c906005906020840190610096565b5050505050610131565b828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f106100d757805160ff1916838001178555610104565b82800160010185558215610104579182015b828111156101045782518255916020019190600101906100e9565b50610110929150610114565b5090565b61012e91905b80821115610110576000815560010161011a565b90565b610644806101406000396000f3006080604052600436106100ae5763ffffffff7c010000000000000000000000000000000000000000000000000000000060003504166306fdde0381146100b3578063095ea7b31461013d57806318160ddd1461017557806323b872dd1461019c57806327e235e3146101c6578063313ce567146101e75780635c6581651461021257806370a082311461023957806395d89b411461025a578063a9059cbb1461026f578063dd62ed3e14610293575b600080fd5b3480156100bf57600080fd5b506100c86102ba565b6040805160208082528351818301528351919283929083019185019080838360005b838110156101025781810151838201526020016100ea565b50505050905090810190601f16801561012f5780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b34801561014957600080fd5b50610161600160a060020a0360043516602435610348565b604080519115158252519081900360200190f35b34801561018157600080fd5b5061018a6103ae565b60408051918252519081900360200190f35b3480156101a857600080fd5b50610161600160a060020a03600435811690602435166044356103b4565b3480156101d257600080fd5b5061018a600160a060020a03600435166104b7565b3480156101f357600080fd5b506101fc6104c9565b6040805160ff9092168252519081900360200190f35b34801561021e57600080fd5b5061018a600160a060020a03600435811690602435166104d2565b34801561024557600080fd5b5061018a600160a060020a03600435166104ef565b34801561026657600080fd5b506100c861050a565b34801561027b57600080fd5b50610161600160a060020a0360043516602435610565565b34801561029f57600080fd5b5061018a600160a060020a03600435811690602435166105ed565b6003805460408051602060026001851615610100026000190190941693909304601f810184900484028201840190925281815292918301828280156103405780601f1061031557610100808354040283529160200191610340565b820191906000526020600020905b81548152906001019060200180831161032357829003601f168201915b505050505081565b336000818152600160209081526040808320600160a060020a038716808552908352818420869055815186815291519394909390927f8c5be1e5ebec7d5bd14f71427d1e84f3dd0314c0f7b2291e5b200ac8c7c3b925928290030190a350600192915050565b60025481565b600160a060020a03831660008181526001602090815260408083203384528252808320549383529082905281205490919083118015906103f45750828110155b15156103ff57600080fd5b600160a060020a038085166000908152602081905260408082208054870190559187168152208054849003905560001981101561046157600160a060020a03851660009081526001602090815260408083203384529091529020805484900390555b83600160a060020a031685600160a060020a03167fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef856040518082815260200191505060405180910390a3506001949350505050565b60006020819052908152604090205481565b60045460ff1681565b600160209081526000928352604080842090915290825290205481565b600160a060020a031660009081526020819052604090205490565b6005805460408051602060026001851615610100026000190190941693909304601f810184900484028201840190925281815292918301828280156103405780601f1061031557610100808354040283529160200191610340565b3360009081526020819052604081205482111561058157600080fd5b3360008181526020818152604080832080548790039055600160a060020a03871680845292819020805487019055805186815290519293927fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef929181900390910190a350600192915050565b600160a060020a039182166000908152600160209081526040808320939094168252919091522054905600a165627a7a72305820a364c08a705d8b29603263ebff0569de6c90b2d665d056a3c77729e2eda923ef0029";
#else
        public static string BYTECODE = "";
#endif
            public EIP20Deployment() : base(BYTECODE)
            {
            }

            public EIP20Deployment(string byteCode) : base(byteCode)
            {
            }

            [Parameter("uint256", "_initialAmount", 1)]
            public virtual BigInteger InitialAmount { get; set; }

            [Parameter("string", "_tokenName", 2)]
            public virtual string TokenName { get; set; }

            [Parameter("uint8", "_decimalUnits", 3)]
            public virtual byte DecimalUnits { get; set; }

            [Parameter("string", "_tokenSymbol", 4)]
            public virtual string TokenSymbol { get; set; }
        }

        [Fact] //e2e test on multicall and tokenLists
        public async void ERC20TokenBalances()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var tokens = await new TokenListService().LoadFromUrl(TokenListSources.UNISWAP);
            var tokensOwned = await web3.Eth.ERC20.GetAllTokenBalancesUsingMultiCallAsync(
                new string[] {"0xBA12222222228d8Ba445958a75a0704d566BF2C8"}, tokens.Where(x => x.ChainId == 1),
                BlockParameter.CreateLatest());
            var tokensWithBalance = tokensOwned.Where(x => x.GetTotalBalance() > 0);
            Assert.True(tokensWithBalance.Any()); //assume any as the address comes from a vault so some of these common tokens will have some values
        }


        public async void ERC20TokenBalancesWithPricesExample()
        {
            var web3 = _ethereumClientIntegrationFixture.GetInfuraWeb3(InfuraNetwork.Mainnet);
            var tokens = await new TokenListService().LoadFromUrl(TokenListSources.UNISWAP);
            var tokensOwned = await web3.Eth.ERC20.GetAllTokenBalancesUsingMultiCallAsync(
                new string[] { "0xBA12222222228d8Ba445958a75a0704d566BF2C8" }, tokens.Where(x => x.ChainId == 1),
                BlockParameter.CreateLatest());
            var tokensWithBalance = tokensOwned.Where(x => x.GetTotalBalance() > 0);
            var allGeckoTokens = GetGeckoTokens();
            var geckoTokenOwnerInfos = tokensWithBalance.Select(x => new GeckoTokenOwnerInfo()
            {
                TokenOwnerInfo = x,
                GeckoToken =
                    allGeckoTokens.FirstOrDefault(y => y.Symbol.ToLower() == x.Token.Symbol.ToLower())
            }).ToList();
            var ids = geckoTokenOwnerInfos.Select(x => x.GeckoToken?.Id).Distinct().ToArray();
            var prices = await GetPrices(ids).ConfigureAwait(false);

            foreach (var geckoTokenOwnerInfo in geckoTokenOwnerInfos)
            {
                if (geckoTokenOwnerInfo.GeckoToken != null)
                {
                    try
                    {
                        var price = prices[geckoTokenOwnerInfo.GeckoToken.Id.ToLower()];
                        geckoTokenOwnerInfo.TokenOwnerInfo.TokenExchangeRate.Add(new TokenExchangeRate() { Currency = "usd", Price = price["usd"] });
                    }
                    catch
                    {

                    }
                }
            }
        }

        //Simple VO to make a mapping of Gecko to Token OwnerInfo
        public class GeckoTokenOwnerInfo
        {
            public GeckoToken GeckoToken { get; set; }
            public TokenOwnerInfo TokenOwnerInfo { get; set; }
        }

        public class GeckoToken
        {
            public string Id { get; set; }
            public string Symbol { get; set; }
        }

        public static string GeckoJson = @"[{'Id':'blockstack','Symbol':'stx'},{'Id':'bitcoin','Symbol':'btc'},{'Id':'liquidity-dividends-protocol','Symbol':'LID'},{'Id':'uma','Symbol':'uma'},{'Id':'uptrennd','Symbol':'1up'},{'Id':'math','Symbol':'math'},{'Id':'dos-network','Symbol':'dos'},{'Id':'xdai-stake','Symbol':'stake'},{'Id':'tellor','Symbol':'trb'},{'Id':'yearn-finance','Symbol':'yfi'},{'Id':'streamr-datacoin','Symbol':'data'},{'Id':'wrapped-nxm','Symbol':'wnxm'},{'Id':'basic-attention-token','Symbol':'bat'},{'Id':'the-abyss','Symbol':'abyss'},{'Id':'decentraland','Symbol':'mana'},{'Id':'xio','Symbol':'xio'},{'Id':'grid','Symbol':'grid'},{'Id':'howdoo','Symbol':'udoo'},{'Id':'curio','Symbol':'cur'},{'Id':'tendies','Symbol':'tend'},{'Id':'numeraire','Symbol':'nmr'},{'Id':'owl','Symbol':'owl'},{'Id':'parachute','Symbol':'par'},{'Id':'reserve','Symbol':'rsv'},{'Id':'bancor','Symbol':'bnt'},{'Id':'sapien','Symbol':'spn'},{'Id':'wrapped-bitcoin','Symbol':'wbtc'},{'Id':'raiden-network','Symbol':'rdn'},{'Id':'toshify-finance','Symbol':'YFT'},{'Id':'idextools','Symbol':'dext'},{'Id':'airswap','Symbol':'ast'},{'Id':'yflink','Symbol':'yfl'},{'Id':'blitzpredict','Symbol':'xbp'},{'Id':'hex','Symbol':'hex'},{'Id':'cream-2','Symbol':'cream'},{'Id':'simple-token','Symbol':'ost'},{'Id':'bilira','Symbol':'tryb'},{'Id':'viberate','Symbol':'vib'},{'Id':'global-digital-content','Symbol':'gdc'},{'Id':'usd-bancor','Symbol':'usdb'},{'Id':'dark-energy-crystals','Symbol':'dec'},{'Id':'q-dao-governance-token-v1-0','Symbol':'qdao'},{'Id':'blockv','Symbol':'vee'},{'Id':'aidcoin','Symbol':'aid'},{'Id':'tokenbox','Symbol':'tbx'},{'Id':'peerex-network','Symbol':'PERX'},{'Id':'rivetz','Symbol':'rvt'},{'Id':'republic-protocol','Symbol':'ren'},{'Id':'oracolxor','Symbol':'xor'},{'Id':'funfair','Symbol':'fun'},{'Id':'civic','Symbol':'cvc'},{'Id':'spankchain','Symbol':'spank'},{'Id':'cap','Symbol':'cap'},{'Id':'v-id-blockchain','Symbol':'vidt'},{'Id':'vision','Symbol':'vsn'},{'Id':'libertas-token','Symbol':'libertas'},{'Id':'foam-protocol','Symbol':'foam'},{'Id':'usdq','Symbol':'usdq'},{'Id':'quant-network','Symbol':'qnt'},{'Id':'zinc','Symbol':'zinc'},{'Id':'ghost-by-mcafee','Symbol':'ghost'},{'Id':'key','Symbol':'key'},{'Id':'mini','Symbol':'mini'},{'Id':'mcdex','Symbol':'mcb'},{'Id':'digix-gold','Symbol':'dgx'},{'Id':'binance-usd','Symbol':'busd'},{'Id':'chainlink','Symbol':'link'},{'Id':'daostack','Symbol':'gen'},{'Id':'bzx-protocol','Symbol':'bzrx'},{'Id':'bluzelle','Symbol':'blz'},{'Id':'trust','Symbol':'trust'},{'Id':'livepeer','Symbol':'lpt'},{'Id':'power-ledger','Symbol':'powr'},{'Id':'dether','Symbol':'DTH'},{'Id':'cosplay-token','Symbol':'cot'},{'Id':'deviantcoin','Symbol':'dev'},{'Id':'cdai','Symbol':'cdai'},{'Id':'mybit-token','Symbol':'myb'},{'Id':'seth','Symbol':'seth'},{'Id':'defipie','Symbol':'PIE'},{'Id':'iexec-rlc','Symbol':'rlc'},{'Id':'linkart','Symbol':'lar'},{'Id':'martexcoin','Symbol':'mxt'},{'Id':'jetswap-token','Symbol':'wings'},{'Id':'smart-mfg','Symbol':'mfg'},{'Id':'gnosis','Symbol':'gno'},{'Id':'sirin-labs-token','Symbol':'srn'},{'Id':'bankroll-vault','Symbol':'vlt'},{'Id':'geeq','Symbol':'GEEQ'},{'Id':'unifi-protocol','Symbol':'up'},{'Id':'holotoken','Symbol':'hot'},{'Id':'polytrade','Symbol':'trade'},{'Id':'props','Symbol':'props'},{'Id':'amon','Symbol':'amn'},{'Id':'status','Symbol':'SNT'},{'Id':'boxx','Symbol':'boxx'},{'Id':'morpheus-network','Symbol':'mrph'},{'Id':'dfohub','Symbol':'buidl'},{'Id':'santiment-network-token','Symbol':'san'},{'Id':'robonomics-network','Symbol':'xrt'},{'Id':'ethlend','Symbol':'lend'},{'Id':'measurable-data-token','Symbol':'mdt'},{'Id':'origin-protocol','Symbol':'ogn'},{'Id':'atlantis-token','Symbol':'atis'},{'Id':'remme','Symbol':'rem'},{'Id':'goldmint','Symbol':'mntp'},{'Id':'unibright','Symbol':'ubt'},{'Id':'dia-data','Symbol':'DIA'},{'Id':'reserve-rights-token','Symbol':'rsr'},{'Id':'penta','Symbol':'pnt'},{'Id':'akropolis','Symbol':'akro'},{'Id':'nervenetwork','Symbol':'nvt'},{'Id':'swipe','Symbol':'sxp'},{'Id':'paxos-standard','Symbol':'pax'},{'Id':'request-network','Symbol':'req'},{'Id':'orion-protocol','Symbol':'orn'},{'Id':'real','Symbol':'real'},{'Id':'kleros','Symbol':'pnk'},{'Id':'lock-token','Symbol':'lock'},{'Id':'deipool','Symbol':'dip'},{'Id':'ocean-protocol','Symbol':'ocean'},{'Id':'strong','Symbol':'strong'},{'Id':'polymath-network','Symbol':'poly'},{'Id':'digital-rand','Symbol':'dzar'},{'Id':'eth-rsi-60-40-yield-set','Symbol':'ethrsiapy'},{'Id':'maker','Symbol':'mkr'},{'Id':'usd-coin','Symbol':'usdc'},{'Id':'pundi-x','Symbol':'npxs'},{'Id':'yfii-finance','Symbol':'yfii'},{'Id':'dxdao','Symbol':'dxd'},{'Id':'meta','Symbol':'mta'},{'Id':'metronome','Symbol':'met'},{'Id':'equus-mining-token','Symbol':'eqmt'},{'Id':'stableusd','Symbol':'USDS'},{'Id':'loom-network-new','Symbol':'loom'},{'Id':'agrinovuscoin','Symbol':'agri'},{'Id':'celsius-degree-token','Symbol':'cel'},{'Id':'tokencard','Symbol':'tkn'},{'Id':'transcodium','Symbol':'tns'},{'Id':'ceek','Symbol':'ceek'},{'Id':'compound-0x','Symbol':'czrx'},{'Id':'cryptofranc','Symbol':'xchf'},{'Id':'rocket-pool','Symbol':'rpl'},{'Id':'perlin','Symbol':'perl'},{'Id':'stonk','Symbol':'stonk'},{'Id':'bitsou','Symbol':'btu'},{'Id':'release-ico-project','Symbol':'rel'},{'Id':'balancer','Symbol':'bal'},{'Id':'band-protocol','Symbol':'band'},{'Id':'pangea','Symbol':'xpat'},{'Id':'loopring','Symbol':'lrc'},{'Id':'ink-protocol','Symbol':'xnk'},{'Id':'meter-governance-mapped-by-meter-io','Symbol':'eMTRG'},{'Id':'kardiachain','Symbol':'kai'},{'Id':'storm','Symbol':'stmx'},{'Id':'aelf','Symbol':'elf'},{'Id':'compound-coin','Symbol':'comp'},{'Id':'havven','Symbol':'snx'},{'Id':'aleph','Symbol':'aleph'},{'Id':'weth','Symbol':'weth'},{'Id':'compound-wrapped-btc','Symbol':'cwbtc'},{'Id':'auctus','Symbol':'auc'},{'Id':'lamden','Symbol':'tau'},{'Id':'quadrant-protocol','Symbol':'equad'},{'Id':'trendering','Symbol':'trnd'},{'Id':'gifto','Symbol':'gto'},{'Id':'zzz-finance','Symbol':'zzz'},{'Id':'trustswap','Symbol':'swap'},{'Id':'nectar-token','Symbol':'nec'},{'Id':'anj','Symbol':'anj'},{'Id':'yffi-finance','Symbol':'yffi'},{'Id':'cbi-index-7','Symbol':'cbix7'},{'Id':'machix','Symbol':'mcx'},{'Id':'omisego','Symbol':'omg'},{'Id':'ong','Symbol':'ong'},{'Id':'ampleforth','Symbol':'ampl'},{'Id':'cindicator','Symbol':'cnd'},{'Id':'fintrux','Symbol':'ftx'},{'Id':'dfohub','Symbol':'buidl'},{'Id':'sociall','Symbol':'scl'},{'Id':'pluton','Symbol':'plu'},{'Id':'tether','Symbol':'usdt'},{'Id':'stasis-eurs','Symbol':'eurs'},{'Id':'kyber-network','Symbol':'kncl'},{'Id':'mainframe','Symbol':'mft'},{'Id':'husd','Symbol':'husd'},{'Id':'karma-dao','Symbol':'karma'},{'Id':'rmpl','Symbol':'rmpl'},{'Id':'shipchain','Symbol':'ship'},{'Id':'pillar','Symbol':'plr'},{'Id':'0x','Symbol':'zrx'},{'Id':'2key','Symbol':'2key'},{'Id':'renbtc','Symbol':'renbtc'},{'Id':'melon','Symbol':'mln'},{'Id':'zippie','Symbol':'zipt'},{'Id':'askobar-network','Symbol':'asko'},{'Id':'ethereum-vault','Symbol':'ethv'},{'Id':'finnexus','Symbol':'fnx'},{'Id':'evo','Symbol':'evo'},{'Id':'flixxo','Symbol':'flixx'},{'Id':'pamp-cc','Symbol':'PAMP'},{'Id':'hedgetrade','Symbol':'hedg'},{'Id':'dmst','Symbol':'dmst'},{'Id':'unicrypt','Symbol':'unc'},{'Id':'unipower','Symbol':'power'},{'Id':'metal','Symbol':'mtl'},{'Id':'enjincoin','Symbol':'enj'},{'Id':'compound-usdt','Symbol':'cusdt'},{'Id':'indorse','Symbol':'ind'},{'Id':'antiample','Symbol':'xamp'},{'Id':'ripio-credit-network','Symbol':'rcn'},{'Id':'trueaud','Symbol':'taud'},{'Id':'truegbp','Symbol':'tgbp'},{'Id':'truehkd','Symbol':'thkd'},{'Id':'gastoken','Symbol':'gst2'},{'Id':'chai','Symbol':'chai'},{'Id':'compound-basic-attention-token','Symbol':'cbat'},{'Id':'compound-sai','Symbol':'csai'},{'Id':'compound-ether','Symbol':'ceth'},{'Id':'compound-usd-coin','Symbol':'cusdc'},{'Id':'compound-augur','Symbol':'crep'},{'Id':'leo-token','Symbol':'leo'},{'Id':'huobi-token','Symbol':'ht'},{'Id':'matic-network','Symbol':'matic'},{'Id':'dai','Symbol':'dai'},{'Id':'sai','Symbol':'sai'},{'Id':'nusd','Symbol':'susd'},{'Id':'seur','Symbol':'seur'},{'Id':'ibtc','Symbol':'iBTC'},{'Id':'sbtc','Symbol':'sbtc'},{'Id':'saud','Symbol':'saud'},{'Id':'scex','Symbol':'scex'},{'Id':'sada','Symbol':'sada'},{'Id':'sdash','Symbol':'sdash'},{'Id':'seos','Symbol':'seos'},{'Id':'setc','Symbol':'setc'},{'Id':'sxmr','Symbol':'sxmr'},{'Id':'sxrp','Symbol':'sxrp'},{'Id':'sxag','Symbol':'sxag'},{'Id':'sltc','Symbol':'sltc'},{'Id':'ieth','Symbol':'ieth'},{'Id':'sdefi','Symbol':'sdefi'},{'Id':'sxau','Symbol':'sxau'},{'Id':'sbnb','Symbol':'sbnb'},{'Id':'sxtz','Symbol':'sxtz'},{'Id':'shiba-link','Symbol':'slink'},{'Id':'ibnb-2','Symbol':'ibnb'},{'Id':'ieos','Symbol':'ieos'},{'Id':'dollars','Symbol':'usdx'},{'Id':'true-usd','Symbol':'tusd'},{'Id':'trustline-network','Symbol':'tln'},{'Id':'lunch-money','Symbol':'lmy'},{'Id':'ybusd','Symbol':'ybusd'},{'Id':'ytusd','Symbol':'ytusd'},{'Id':'blockchain-certified-data-token','Symbol':'bcdt'},{'Id':'lendroid-support-token','Symbol':'lst'},{'Id':'marketpeak','Symbol':'peak'},{'Id':'pantos','Symbol':'pan'},{'Id':'gemini-dollar','Symbol':'gusd'},{'Id':'proton','Symbol':'xpr'},{'Id':'keep-network','Symbol':'keep'},{'Id':'renzec','Symbol':'renzec'},{'Id':'renbch','Symbol':'renbch'},{'Id':'t-bitcoin','Symbol':'tbtc'},{'Id':'huobi-btc','Symbol':'hbtc'},{'Id':'shuffle-monster','Symbol':'shuf'},{'Id':'donut','Symbol':'donut'},{'Id':'chi-gastoken','Symbol':'chi'},{'Id':'switch','Symbol':'esh'},{'Id':'pax-gold','Symbol':'paxg'},{'Id':'0xmonero','Symbol':'0xmr'},{'Id':'storj','Symbol':'storj'},{'Id':'salt','Symbol':'salt'},{'Id':'curve-fi-ydai-yusdc-yusdt-ytusd','Symbol':'yCurve'},{'Id':'rarible','Symbol':'rari'},{'Id':'pareto-network','Symbol':'pareto'},{'Id':'plutus-defi','Symbol':'plt'},{'Id':'ptokens-btc','Symbol':'pbtc'},{'Id':'serum','Symbol':'srm'},{'Id':'autonio','Symbol':'niox'},{'Id':'defi-stoa','Symbol':'sta'},{'Id':'falcon-token','Symbol':'fnt'},{'Id':'yam-2','Symbol':'yam'},{'Id':'addax','Symbol':'adx'},{'Id':'curve-dao-token','Symbol':'crv'},{'Id':'darwinia-network-native-token','Symbol':'ring'},{'Id':'cartesi','Symbol':'ctsi'},{'Id':'unilayer','Symbol':'layer'},{'Id':'degenerator','Symbol':'meme'},{'Id':'origintrail','Symbol':'trac'},{'Id':'yam-v2','Symbol':'YAMv2'},{'Id':'jarvis-reward-token','Symbol':'jrt'},{'Id':'neutrino','Symbol':'usdn'},{'Id':'parsiq','Symbol':'prq'},{'Id':'hakka-finance','Symbol':'hakka'},{'Id':'robonomics-web-services','Symbol':'rws'},{'Id':'growth-defi','Symbol':'gro'},{'Id':'concentrated-voting-power','Symbol':'cvp'},{'Id':'ethopt','Symbol':'opt'},{'Id':'sushi','Symbol':'sushi'},{'Id':'stacktical','Symbol':'dsla'},{'Id':'swapfolio','Symbol':'swfl'},{'Id':'fsw-token','Symbol':'fsw'},{'Id':'akropolis-delphi','Symbol':'adel'},{'Id':'swerve-dao','Symbol':'swrv'},{'Id':'multiplier','Symbol':'mxx'},{'Id':'genesis-vision','Symbol':'gvt'},{'Id':'step-finance','Symbol':'step'},{'Id':'safe-coin','Symbol':'safe'},{'Id':'predix-network','Symbol':'prdx'},{'Id':'defipulse-index','Symbol':'dpi'},{'Id':'aavegotchi','Symbol':'ghst'},{'Id':'unicorn-token','Symbol':'uni'},{'Id':'game-x-coin','Symbol':'gxc'},{'Id':'pickle-finance','Symbol':'pickle'},{'Id':'frontier-token','Symbol':'front'},{'Id':'dhedge-dao','Symbol':'dht'},{'Id':'harvest-finance','Symbol':'farm'},{'Id':'golff','Symbol':'gof'},{'Id':'xbtc','Symbol':'xbtc'},{'Id':'origin-dollar','Symbol':'ousd'},{'Id':'aave','Symbol':'aave'},{'Id':'dodo','Symbol':'dodo'},{'Id':'safe2','Symbol':'safe2'},{'Id':'spaceswap-shake','Symbol':'shake'},{'Id':'spaceswap-milk2','Symbol':'milk2'},{'Id':'cvault-finance','Symbol':'core'},{'Id':'perpetual-protocol','Symbol':'perp'},{'Id':'value-liquidity','Symbol':'value'},{'Id':'sparkle','Symbol':'sprkl'},{'Id':'usdk','Symbol':'usdk'},{'Id':'swag-finance','Symbol':'swag'},{'Id':'piedao-dough-v2','Symbol':'dough'},{'Id':'kush-finance','Symbol':'kseed'},{'Id':'ccomp','Symbol':'ccomp'},{'Id':'compound-uniswap','Symbol':'cuni'},{'Id':'quras-token','Symbol':'xqc'},{'Id':'master-usd','Symbol':'musd'},{'Id':'zeroswap','Symbol':'zee'},{'Id':'hegic','Symbol':'hegic'},{'Id':'definer','Symbol':'fin'},{'Id':'astro','Symbol':'astro'},{'Id':'amp-token','Symbol':'amp'},{'Id':'barnbridge','Symbol':'bond'},{'Id':'antcoin','Symbol':'ant'},{'Id':'fuse-network-token','Symbol':'fuse'},{'Id':'empty-set-dollar','Symbol':'esd'},{'Id':'keep3rv1','Symbol':'kp3r'},{'Id':'defidollar','Symbol':'dusd'},{'Id':'aurora-dao','Symbol':'idex'},{'Id':'nix-bridge-token','Symbol':'voice'},{'Id':'hermez-network-token','Symbol':'hez'},{'Id':'surfexutilitytoken','Symbol':'surf'},{'Id':'wrapped-anatha','Symbol':'wanatha'},{'Id':'audius','Symbol':'audio'},{'Id':'atari','Symbol':'atri'},{'Id':'index-cooperative','Symbol':'index'},{'Id':'powertrade-fuel','Symbol':'ptf'},{'Id':'defidollar-dao','Symbol':'dfd'},{'Id':'apy-finance','Symbol':'apy'},{'Id':'geyser','Symbol':'gysr'},{'Id':'keep4r','Symbol':'kp4r'},{'Id':'axie-infinity','Symbol':'axs'},{'Id':'smart-valor','Symbol':'valor'},{'Id':'allianceblock','Symbol':'albt'},{'Id':'tomoe','Symbol':'tomoe'},{'Id':'lua-token','Symbol':'lua'},{'Id':'holyheld','Symbol':'holy'},{'Id':'polkastarter','Symbol':'pols'},{'Id':'rio-defi','Symbol':'rfuel'},{'Id':'unlend-finance','Symbol':'uft'},{'Id':'lgcy-network','Symbol':'lgcy'},{'Id':'rope-token','Symbol':'rope'},{'Id':'plotx','Symbol':'plot'},{'Id':'keysians-network','Symbol':'ken'},{'Id':'nsure-network','Symbol':'nsure'},{'Id':'chronobank','Symbol':'time'},{'Id':'saffron-finance','Symbol':'sfi'},{'Id':'88mph','Symbol':'mph'},{'Id':'oro','Symbol':'oro'},{'Id':'e-radix','Symbol':'exrd'},{'Id':'boosted-finance','Symbol':'boost'},{'Id':'dforce-token','Symbol':'df'},{'Id':'synlev','Symbol':'syn'},{'Id':'lto-network','Symbol':'lto'},{'Id':'synth-soil','Symbol':'soil'},{'Id':'cache-gold','Symbol':'cgt'},{'Id':'nucypher','Symbol':'nu'},{'Id':'octree','Symbol':'oct'},{'Id':'quiverx','Symbol':'qrx'},{'Id':'bitsong','Symbol':'btsg'},{'Id':'radium','Symbol':'val'},{'Id':'api3','Symbol':'api3'},{'Id':'basis-cash','Symbol':'bac'},{'Id':'basis-share','Symbol':'bas'},{'Id':'power-index-pool-token','Symbol':'pipt'},{'Id':'megacryptopolis','Symbol':'mega'},{'Id':'base-protocol','Symbol':'base'},{'Id':'bondly','Symbol':'bondly'},{'Id':'neutrino-system-base-token','Symbol':'nsbt'},{'Id':'nexo','Symbol':'nexo'},{'Id':'aave-aave','Symbol':'aAAVE'},{'Id':'aave-bat','Symbol':'abat'},{'Id':'aave-busd','Symbol':'abusd'},{'Id':'aave-dai','Symbol':'adai'},{'Id':'aave-enj','Symbol':'aenj'},{'Id':'aave-knc','Symbol':'aknc'},{'Id':'aave-link','Symbol':'alink'},{'Id':'aave-mana','Symbol':'amana'},{'Id':'aave-mkr','Symbol':'amkr'},{'Id':'aave-ren','Symbol':'aren'},{'Id':'aave-snx','Symbol':'asnx'},{'Id':'aave-susd','Symbol':'asusd'},{'Id':'aave-tusd','Symbol':'atusd'},{'Id':'aave-uni','Symbol':'auni'},{'Id':'aave-usdc','Symbol':'ausdc'},{'Id':'aave-usdt','Symbol':'ausdt'},{'Id':'aave-wbtc','Symbol':'awbtc'},{'Id':'aave-weth','Symbol':'aweth'},{'Id':'aave-yfi','Symbol':'aYFI'},{'Id':'aave-zrx','Symbol':'azrx'},{'Id':'coinlion','Symbol':'lion'},{'Id':'zlot','Symbol':'zlot'},{'Id':'ecofi','Symbol':'eco'},{'Id':'utrust','Symbol':'utk'},{'Id':'badger-dao','Symbol':'badger'},{'Id':'golden-ratio-token','Symbol':'grt'},{'Id':'lido-dao','Symbol':'ldo'},{'Id':'tornado-cash','Symbol':'torn'},{'Id':'staked-ether','Symbol':'steth'},{'Id':'mahadao','Symbol':'maha'},{'Id':'marlin','Symbol':'pond'},{'Id':'frax-share','Symbol':'fxs'},{'Id':'spice','Symbol':'spice'},{'Id':'1inch','Symbol':'1inch'},{'Id':'plasma-finance','Symbol':'ppay'},{'Id':'mithril-share','Symbol':'mis'},{'Id':'basiscoin-share','Symbol':'bcs'},{'Id':'exeedme','Symbol':'xed'},{'Id':'wozx','Symbol':'wozx'},{'Id':'defi-nation-signals-dao','Symbol':'dsd'},{'Id':'fox-finance','Symbol':'fox'},{'Id':'cover-protocol','Symbol':'cover'},{'Id':'wise-token11','Symbol':'wise'},{'Id':'fera','Symbol':'fera'},{'Id':'furucombo','Symbol':'combo'},{'Id':'usdfreeliquidity','Symbol':'usdfl'},{'Id':'fetch-ai','Symbol':'fet'},{'Id':'pha','Symbol':'pha'},{'Id':'pbtc35a','Symbol':'pbtc35a'},{'Id':'frax','Symbol':'frax'},{'Id':'injective-protocol','Symbol':'inj'},{'Id':'legolas-exchange','Symbol':'lgo'},{'Id':'yield','Symbol':'yld'},{'Id':'cyberfi','Symbol':'cfi'},{'Id':'rari-governance-token','Symbol':'rgt'},{'Id':'rook','Symbol':'rook'},{'Id':'yield-optimization-platform','Symbol':'yop'},{'Id':'nftx','Symbol':'nftx'},{'Id':'robbocoach','Symbol':'rbc'},{'Id':'stake-dao','Symbol':'sdt'},{'Id':'ethos','Symbol':'vgx'},{'Id':'debase','Symbol':'debase'},{'Id':'ankr','Symbol':'ankr'},{'Id':'thorchain','Symbol':'rune'},{'Id':'bao-finance','Symbol':'bao'},{'Id':'reef-finance','Symbol':'reef'},{'Id':'truebit-protocol','Symbol':'tru'},{'Id':'indexed-finance','Symbol':'ndx'},{'Id':'benchmark-protocol','Symbol':'mark'},{'Id':'zero-exchange','Symbol':'zero'},{'Id':'octofi','Symbol':'octo'},{'Id':'oraichain-token','Symbol':'orai'},{'Id':'duckdaodime','Symbol':'ddim'},{'Id':'birdchain','Symbol':'bird'},{'Id':'spacechain','Symbol':'spc'},{'Id':'ramp','Symbol':'ramp'},{'Id':'stabilize','Symbol':'stbz'},{'Id':'insured-finance','Symbol':'infi'},{'Id':'crypto-com-chain','Symbol':'cro'},{'Id':'lukso-token','Symbol':'lyxe'},{'Id':'terra-virtua-kolect','Symbol':'tvk'},{'Id':'digg','Symbol':'digg'},{'Id':'freeliquid','Symbol':'fl'},{'Id':'alpha-finance','Symbol':'alpha'},{'Id':'cudos','Symbol':'cudos'},{'Id':'dexe','Symbol':'dexe'},{'Id':'san-diego-coin','Symbol':'sand'},{'Id':'covir','Symbol':'cvr'},{'Id':'typhoon-cash','Symbol':'phoon'},{'Id':'farmer-defi','Symbol':'frm'},{'Id':'polkabridge','Symbol':'pbr'},{'Id':'snowblossom','Symbol':'snow'},{'Id':'tosdis','Symbol':'dis'},{'Id':'poolz-finance','Symbol':'poolz'},{'Id':'zkswap','Symbol':'zks'},{'Id':'armor','Symbol':'armor'},{'Id':'armor-nxm','Symbol':'arnxm'},{'Id':'opium','Symbol':'opium'},{'Id':'yearn-ecosystem-token-index','Symbol':'yeti'},{'Id':'assy-index','Symbol':'assy'},{'Id':'defi-yield-protocol','Symbol':'dyp'},{'Id':'yusdc-busd-pool','Symbol':'yusdc'},{'Id':'veth2','Symbol':'veth2'},{'Id':'aave-eth-v1','Symbol':'aeth'},{'Id':'cream-eth2','Symbol':'creth2'},{'Id':'fantom','Symbol':'ftm'},{'Id':'prosper','Symbol':'pros'},{'Id':'fastswap','Symbol':'fast'},{'Id':'reflect-finance','Symbol':'rfi'},{'Id':'terrausd','Symbol':'ust'},{'Id':'rendoge','Symbol':'rendoge'},{'Id':'mir-coin','Symbol':'mir'},{'Id':'flex-coin','Symbol':'flex'},{'Id':'metric-exchange','Symbol':'metric'},{'Id':'chartex','Symbol':'chart'},{'Id':'bridge-mutual','Symbol':'bmi'},{'Id':'digitex-futures-exchange','Symbol':'dgtx'},{'Id':'millimeter','Symbol':'mm'},{'Id':'tokenlon','Symbol':'lon'},{'Id':'archer-dao-governance-token','Symbol':'arch'},{'Id':'biblepay','Symbol':'bbp'},{'Id':'sx-network','Symbol':'sx'},{'Id':'lattice-token','Symbol':'ltx'},{'Id':'clash-token','Symbol':'sct'},{'Id':'leverj-gluon','Symbol':'l2'},{'Id':'onix','Symbol':'onx'},{'Id':'beefy-finance','Symbol':'bifi'},{'Id':'stafi','Symbol':'fis'},{'Id':'lina','Symbol':'lina'},{'Id':'oin-finance','Symbol':'oin'},{'Id':'xinchb','Symbol':'xINCHb'},{'Id':'xincha','Symbol':'xINCHa'},{'Id':'crowns','Symbol':'cws'},{'Id':'shiba-inu','Symbol':'shib'},{'Id':'portion','Symbol':'prt'},{'Id':'name-changing-token','Symbol':'nct'},{'Id':'muse-2','Symbol':'muse'},{'Id':'maps','Symbol':'maps'},{'Id':'build-finance','Symbol':'build'},{'Id':'gourmetgalaxy','Symbol':'gum'},{'Id':'defi-top-5-tokens-index','Symbol':'defi5'},{'Id':'cryptocurrency-top-10-tokens-index','Symbol':'cc10'},{'Id':'tixl-new','Symbol':'txl'},{'Id':'razor-network','Symbol':'razor'},{'Id':'strudel-finance','Symbol':'trdl'},{'Id':'yvs-finance','Symbol':'yvs'},{'Id':'bundles','Symbol':'bund'},{'Id':'sashimi','Symbol':'sashimi'},{'Id':'hedget','Symbol':'hget'},{'Id':'option-room','Symbol':'room'},{'Id':'wrapped-crescofin','Symbol':'wcres'},{'Id':'gala','Symbol':'gala'},{'Id':'seigniorage-shares','Symbol':'share'},{'Id':'unistake','Symbol':'unistake'},{'Id':'azuki','Symbol':'azuki'},{'Id':'coin-artist','Symbol':'coin'},{'Id':'dextf','Symbol':'dextf'},{'Id':'mp3','Symbol':'mp3'},{'Id':'litentry','Symbol':'lit'},{'Id':'terra-luna','Symbol':'luna'},{'Id':'easyfi','Symbol':'ez'},{'Id':'sync-network','Symbol':'sync'},{'Id':'finxflo','Symbol':'fxf'},{'Id':'bot-ocean','Symbol':'bots'},{'Id':'mar-network','Symbol':'mars'},{'Id':'nftlootbox','Symbol':'loot'},{'Id':'dlp-duck-token','Symbol':'duck'},{'Id':'the-famous-token','Symbol':'tft'},{'Id':'everid','Symbol':'id'},{'Id':'skale','Symbol':'skl'},{'Id':'dao-maker','Symbol':'dao'},{'Id':'bitenium-token','Symbol':'bt'},{'Id':'flash','Symbol':'flash'},{'Id':'butterfly-protocol-2','Symbol':'bfly'},{'Id':'safedot','Symbol':'sdot'},{'Id':'scomp','Symbol':'scomp'},{'Id':'saave','Symbol':'saave'},{'Id':'idot','Symbol':'idot'},{'Id':'soft-yearn','Symbol':'syfi'},{'Id':'suni','Symbol':'suni'},{'Id':'sren','Symbol':'sren'},{'Id':'umbrella-network','Symbol':'umb'},{'Id':'ichi-farm','Symbol':'ichi'},{'Id':'usdp','Symbol':'usdp'},{'Id':'unisocks','Symbol':'socks'},{'Id':'stsla','Symbol':'stsla'},{'Id':'marginswap','Symbol':'mfi'},{'Id':'envion','Symbol':'evn'},{'Id':'klondike-finance','Symbol':'klon'},{'Id':'klondike-btc','Symbol':'kbtc'},{'Id':'open-governance-token','Symbol':'open'},{'Id':'cryptotask-2','Symbol':'ctask'},{'Id':'pylon-finance','Symbol':'pylon'},{'Id':'peanut','Symbol':'nux'},{'Id':'depay','Symbol':'depay'},{'Id':'fyooz','Symbol':'fyz'},{'Id':'scifi-index','Symbol':'scifi'},{'Id':'0chain','Symbol':'zcn'},{'Id':'unicrypt-2','Symbol':'uncx'},{'Id':'warp-finance','Symbol':'warp'},{'Id':'idle','Symbol':'idle'},{'Id':'sparkpoint','Symbol':'srk'},{'Id':'glitch-protocol','Symbol':'glch'},{'Id':'unimex-network','Symbol':'umx'},{'Id':'whiteheart','Symbol':'white'},{'Id':'dent','Symbol':'dent'},{'Id':'zenfuse','Symbol':'zefu'},{'Id':'moontools','Symbol':'moons'},{'Id':'sake-token','Symbol':'sake'},{'Id':'micro-bitcoin-finance','Symbol':'mbtc'},{'Id':'vesper-finance','Symbol':'vsp'},{'Id':'sharedstake-governance-token','Symbol':'sgt'},{'Id':'shroom-finance','Symbol':'shroom'},{'Id':'gameswap-org','Symbol':'gswap'},{'Id':'fudfinance','Symbol':'fud'},{'Id':'rai','Symbol':'rai'},{'Id':'unidex','Symbol':'unidx'},{'Id':'doki-doki-finance','Symbol':'doki'},{'Id':'essentia','Symbol':'ess'},{'Id':'gather','Symbol':'gth'},{'Id':'offshift','Symbol':'xft'},{'Id':'seen','Symbol':'seen'},{'Id':'ethart','Symbol':'arte'},{'Id':'alpaca','Symbol':'alpa'},{'Id':'utu-coin','Symbol':'utu'},{'Id':'achain-coin','Symbol':'ac'},{'Id':'royale','Symbol':'roya'},{'Id':'premia','Symbol':'premia'},{'Id':'rigel-finance','Symbol':'rigel'},{'Id':'poolcoin','Symbol':'pool'},{'Id':'smartcredit-token','Symbol':'smartcredit'},{'Id':'rootkit','Symbol':'root'},{'Id':'revv','Symbol':'revv'},{'Id':'phoenixdao','Symbol':'phnx'},{'Id':'dexkit','Symbol':'kit'},{'Id':'wootrade-network','Symbol':'woo'},{'Id':'modefi','Symbol':'mod'},{'Id':'hydro','Symbol':'hydro'},{'Id':'mask-network','Symbol':'mask'},{'Id':'anyswap','Symbol':'any'},{'Id':'rally-2','Symbol':'rly'},{'Id':'kira-network','Symbol':'kex'},{'Id':'ultra','Symbol':'uos'},{'Id':'geocoin','Symbol':'geo'},{'Id':'get-token','Symbol':'get'},{'Id':'apoyield','Symbol':'soul'},{'Id':'unifi','Symbol':'unifi'},{'Id':'derivadao','Symbol':'ddx'},{'Id':'quick','Symbol':'quick'},{'Id':'redfox-labs-2','Symbol':'rfox'},{'Id':'monacoin','Symbol':'mona'},{'Id':'hybrix','Symbol':'hy'},{'Id':'supercoin','Symbol':'super'},{'Id':'wrapped-dgld','Symbol':'wdgld'},{'Id':'coinshares-gold-and-cryptoassets-index-lite','Symbol':'cgi'},{'Id':'mushroom','Symbol':'mush'},{'Id':'launchpool','Symbol':'lpool'},{'Id':'xtake','Symbol':'xtk'},{'Id':'signal-token','Symbol':'sig'},{'Id':'hopr','Symbol':'hopr'},{'Id':'foundrydao-logistics','Symbol':'fry'},{'Id':'gamecredits','Symbol':'game'},{'Id':'grap-finance','Symbol':'grap'},{'Id':'render-token','Symbol':'rndr'},{'Id':'ovr','Symbol':'ovr'},{'Id':'mettalex','Symbol':'mtlx'},{'Id':'polkamarkets','Symbol':'polk'},{'Id':'bancor-governance-token','Symbol':'vbnt'},{'Id':'nord-finance','Symbol':'nord'},{'Id':'shadows','Symbol':'dows'},{'Id':'mint-club','Symbol':'mint'},{'Id':'degen-index','Symbol':'degen'},{'Id':'bifrost','Symbol':'bfc'},{'Id':'siren','Symbol':'si'},{'Id':'font','Symbol':'font'},{'Id':'moon','Symbol':'moon'},{'Id':'jupiter','Symbol':'jup'},{'Id':'sentiment-token','Symbol':'sent'},{'Id':'dego-finance','Symbol':'dego'},{'Id':'decentral-games','Symbol':'dg'},{'Id':'sota-finance','Symbol':'sota'},{'Id':'noderunners','Symbol':'ndr'},{'Id':'daofi','Symbol':'daofi'},{'Id':'radicle','Symbol':'rad'},{'Id':'alchemix','Symbol':'alcx'},{'Id':'bankless-dao','Symbol':'bank'},{'Id':'antimatter','Symbol':'matter'},{'Id':'fractal','Symbol':'fcl'},{'Id':'verasity','Symbol':'vra'},{'Id':'nft-index','Symbol':'nfti'},{'Id':'bidipass','Symbol':'bdp'},{'Id':'earnscoin','Symbol':'ern'},{'Id':'kylin-network','Symbol':'kyl'},{'Id':'robot','Symbol':'robot'},{'Id':'etha-lend','Symbol':'etha'},{'Id':'paint','Symbol':'paint'},{'Id':'ruler-protocol','Symbol':'ruler'},{'Id':'xfund','Symbol':'xfund'},{'Id':'balpha','Symbol':'balpha'},{'Id':'dea','Symbol':'dea'},{'Id':'chiliz','Symbol':'chz'},{'Id':'inverse-finance','Symbol':'inv'},{'Id':'govi','Symbol':'govi'},{'Id':'bet-protocol','Symbol':'bepro'},{'Id':'non-fungible-yearn','Symbol':'nfy'},{'Id':'blank','Symbol':'blank'},{'Id':'smol','Symbol':'smol'},{'Id':'definitex','Symbol':'dfx'},{'Id':'b20','Symbol':'b20'},{'Id':'tapmydata','Symbol':'tap'},{'Id':'taco-finance','Symbol':'taco'},{'Id':'fyznft','Symbol':'fyznft'},{'Id':'swgtoken','Symbol':'swg'},{'Id':'dusk-network','Symbol':'dusk'},{'Id':'lcx','Symbol':'lcx'},{'Id':'insurace','Symbol':'insur'},{'Id':'tozex','Symbol':'toz'},{'Id':'visor','Symbol':'visr'},{'Id':'aluna','Symbol':'aln'},{'Id':'chain-guardians','Symbol':'cgg'},{'Id':'crust-network','Symbol':'cru'},{'Id':'my-neighbor-alice','Symbol':'alice'},{'Id':'tower','Symbol':'tower'},{'Id':'polyyield-token','Symbol':'yield'},{'Id':'konomi-network','Symbol':'kono'},{'Id':'soar-2','Symbol':'soar'},{'Id':'dovu','Symbol':'dov'},{'Id':'circleex','Symbol':'cx'},{'Id':'juggernaut','Symbol':'jgn'},{'Id':'hoge-finance','Symbol':'hoge'},{'Id':'changenow','Symbol':'now'},{'Id':'connect-financial','Symbol':'cnfi'},{'Id':'hodltree','Symbol':'htre'},{'Id':'polkafoundry','Symbol':'pkf'},{'Id':'exrt-network','Symbol':'exrt'},{'Id':'deri-protocol','Symbol':'deri'},{'Id':'blockchain-cuties-universe-governance','Symbol':'bcug'},{'Id':'labs-group','Symbol':'labs'},{'Id':'kine-protocol','Symbol':'kine'},{'Id':'hapi','Symbol':'hapi'},{'Id':'k21','Symbol':'k21'},{'Id':'union-protocol-governance-token','Symbol':'unn'},{'Id':'habitat','Symbol':'hbt'},{'Id':'cash-tech','Symbol':'cate'},{'Id':'doraemoon','Symbol':'dora'},{'Id':'sifchain','Symbol':'erowan'},{'Id':'sentivate','Symbol':'sntvt'},{'Id':'chain-games','Symbol':'chain'},{'Id':'xdefi-governance-token','Symbol':'xdex'},{'Id':'upbots','Symbol':'ubxt'},{'Id':'graphlinq-protocol','Symbol':'glq'},{'Id':'lympo','Symbol':'lym'},{'Id':'vidya','Symbol':'vidya'},{'Id':'fireball','Symbol':'fire'},{'Id':'dafi-protocol','Symbol':'dafi'},{'Id':'oddz','Symbol':'oddz'},{'Id':'paypolitan-token','Symbol':'epan'},{'Id':'ara-token','Symbol':'ara'},{'Id':'2gether-2','Symbol':'2gt'},{'Id':'venus-eth','Symbol':'veth'},{'Id':'coinfirm-amlt','Symbol':'amlt'},{'Id':'volentix-vtx','Symbol':'vtx'},{'Id':'vvsp','Symbol':'vvsp'},{'Id':'tribe-2','Symbol':'tribe'},{'Id':'fei-protocol','Symbol':'fei'},{'Id':'xsgd','Symbol':'xsgd'},{'Id':'aioz-network','Symbol':'aioz'},{'Id':'spheroid-universe','Symbol':'sph'},{'Id':'pocmon','Symbol':'pmon'},{'Id':'sylo','Symbol':'sylo'},{'Id':'overline-emblem','Symbol':'emb'},{'Id':'ureeqa','Symbol':'urqa'},{'Id':'linkpool','Symbol':'lpl'},{'Id':'curate','Symbol':'xcur'},{'Id':'cook','Symbol':'cook'},{'Id':'cellframe','Symbol':'cell'},{'Id':'mad-network','Symbol':'mad'},{'Id':'convergence','Symbol':'conv'},{'Id':'swarm','Symbol':'swm'},{'Id':'eddaswap','Symbol':'edda'},{'Id':'tidal-finance','Symbol':'tidal'},{'Id':'deracoin','Symbol':'drc'},{'Id':'xyo-network','Symbol':'xyo'},{'Id':'arcona','Symbol':'arcona'},{'Id':'vulcan-forged','Symbol':'pyr'},{'Id':'roobee','Symbol':'roobee'},{'Id':'deeper-network','Symbol':'dpr'},{'Id':'gains','Symbol':'gains'},{'Id':'liquity-usd','Symbol':'lusd'},{'Id':'equalizer','Symbol':'eqz'},{'Id':'genesis-shards','Symbol':'gs'},{'Id':'internxt','Symbol':'inxt'},{'Id':'olympus','Symbol':'ohm'},{'Id':'raze-network','Symbol':'raze'},{'Id':'alchemist','Symbol':'mist'},{'Id':'cardstarter','Symbol':'cards'},{'Id':'ethbox-token','Symbol':'ebox'},{'Id':'presearch','Symbol':'pre'},{'Id':'ethereum-push-notification-service','Symbol':'push'},{'Id':'zoracles','Symbol':'zora'},{'Id':'boson-protocol','Symbol':'boson'},{'Id':'universal-basic-income','Symbol':'ubi'},{'Id':'total-crypto-market-cap-token','Symbol':'tcap'},{'Id':'basketdao','Symbol':'bask'},{'Id':'nkn','Symbol':'nkn'},{'Id':'the-4th-pillar','Symbol':'four'},{'Id':'dentacoin','Symbol':'dcn'},{'Id':'ampleforth-governance-token','Symbol':'forth'},{'Id':'s1inch','Symbol':'s1inch'},{'Id':'srune','Symbol':'srune'},{'Id':'scrv','Symbol':'scrv'},{'Id':'snflx','Symbol':'snflx'},{'Id':'sfb','Symbol':'sfb'},{'Id':'sgoog','Symbol':'sgoog'},{'Id':'samzn','Symbol':'samzn'},{'Id':'blind-boxes','Symbol':'bles'},{'Id':'stakewise','Symbol':'swise'},{'Id':'kyber-network-crystal','Symbol':'knc'},{'Id':'yaxis','Symbol':'yaxis'},{'Id':'orbs','Symbol':'orbs'},{'Id':'wirex','Symbol':'wxt'},{'Id':'shincoin','Symbol':'scoin'},{'Id':'baguette','Symbol':'bag'},{'Id':'occamfi','Symbol':'occ'},{'Id':'illuvium','Symbol':'ilv'},{'Id':'unfederalreserve','Symbol':'ersdl'},{'Id':'ice-token','Symbol':'ice'},{'Id':'xend-finance','Symbol':'xend'},{'Id':'unmarshal','Symbol':'marsh'},{'Id':'aga-token','Symbol':'aga'},{'Id':'circuits-of-value','Symbol':'coval'},{'Id':'tenset','Symbol':'10set'},{'Id':'bonfi','Symbol':'bnf'},{'Id':'kin','Symbol':'kin'},{'Id':'golem','Symbol':'glm'},{'Id':'telcoin','Symbol':'tel'},{'Id':'unlock-protocol','Symbol':'udt'},{'Id':'pendle','Symbol':'pendle'},{'Id':'waxe','Symbol':'waxe'},{'Id':'coinstarter','Symbol':'stc'},{'Id':'route','Symbol':'route'},{'Id':'nahmii','Symbol':'nii'},{'Id':'paid-network','Symbol':'paid'},{'Id':'keytango','Symbol':'tango'},{'Id':'splyt','Symbol':'shopx'},{'Id':'ares-protocol','Symbol':'ares'},{'Id':'aga-rewards-2','Symbol':'agar'},{'Id':'cryptex-finance','Symbol':'ctx'},{'Id':'shih-tzu','Symbol':'shih'},{'Id':'somidax','Symbol':'smdx'},{'Id':'kishu-inu','Symbol':'kishu'},{'Id':'feg-token','Symbol':'feg'},{'Id':'stobox-token','Symbol':'stbu'},{'Id':'o3-swap','Symbol':'o3'},{'Id':'woofy','Symbol':'woofy'},{'Id':'shibaken-finance','Symbol':'shibaken'},{'Id':'kirobo','Symbol':'kiro'},{'Id':'convex-finance','Symbol':'cvx'},{'Id':'seedswap','Symbol':'snft'},{'Id':'8pay','Symbol':'8pay'},{'Id':'game','Symbol':'gtc'},{'Id':'graviton','Symbol':'gton'},{'Id':'alchemix-usd','Symbol':'alusd'},{'Id':'sarcophagus','Symbol':'sarco'},{'Id':'terablock','Symbol':'tbc'},{'Id':'value-usd','Symbol':'vusd'},{'Id':'hokkaidu-inu','Symbol':'hokk'},{'Id':'boringdao-[old]','Symbol':'bor'},{'Id':'zoo-token','Symbol':'zoot'},{'Id':'dogelon-mars','Symbol':'elon'},{'Id':'superbid','Symbol':'superbid'},{'Id':'nft-tone','Symbol':'tone'},{'Id':'smartkey','Symbol':'skey'},{'Id':'nimbus','Symbol':'nbu'},{'Id':'leash','Symbol':'leash'},{'Id':'district0x','Symbol':'dnt'},{'Id':'defi-factory-token','Symbol':'deft'},{'Id':'dfyn-network','Symbol':'dfyn'},{'Id':'metaverse-index','Symbol':'mvi'},{'Id':'akita-inu','Symbol':'akita'},{'Id':'liquity','Symbol':'lqty'},{'Id':'verox','Symbol':'vrx'},{'Id':'baby-bitcoin','Symbol':'bbtc'},{'Id':'munch-token','Symbol':'munch'},{'Id':'bezoge-earth','Symbol':'bezoge'},{'Id':'island-coin','Symbol':'isle'},{'Id':'bitcashpay','Symbol':'bcp'},{'Id':'ethereummax','Symbol':'emax'},{'Id':'instadapp','Symbol':'inst'},{'Id':'cavapoo','Symbol':'cava'},{'Id':'swapp','Symbol':'swapp'},{'Id':'dvision-network','Symbol':'dvi'},{'Id':'cad-coin','Symbol':'cadc'},{'Id':'arc-governance','Symbol':'arcx'},{'Id':'amun-defi-index','Symbol':'dfi'},{'Id':'amun-defi-momentum-index','Symbol':'dmx'},{'Id':'xsushi','Symbol':'xsushi'},{'Id':'nxm','Symbol':'nxm'},{'Id':'unit-protocol','Symbol':'col'},{'Id':'auction','Symbol':'auction'},{'Id':'singularitynet','Symbol':'agix'},{'Id':'olyseum','Symbol':'oly'},{'Id':'unizen','Symbol':'zcx'},{'Id':'fnkcom','Symbol':'fnk'},{'Id':'gerowallet','Symbol':'gero'},{'Id':'unobtanium','Symbol':'uno'}]";

        public List<GeckoToken> GetGeckoTokens()
        {
            return JsonConvert.DeserializeObject<List<GeckoToken>>(GeckoJson);
        }

        public Task<Dictionary<string, Dictionary<string, decimal>>> GetPrices(params string[] geckoIds)
        {
            var idsCombined = String.Join(",", geckoIds);
            return LoadFromUrl<Dictionary<string, Dictionary<string, decimal>>>("https://api.coingecko.com/api/v3/simple/price?ids=" + idsCombined + "&vs_currencies=usd");
        }


        public async Task<T> LoadFromUrl<T>(string url)
        {
            var client = new HttpClient();
            var json = await client.GetStringAsync(url);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}